using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25_Vanilla
{
    internal sealed class Session : L3Channel, IDisposable
    {
        private const string PROMPT = "\0\r\n> ";
        private const string ERROR = "\0Error: ";
        private const string NOT_FOUND = "\0Not found: ";
        private readonly LocalEndpoint m_localEndpoint;
        private readonly IL3Channel m_receiver;
        private bool m_connected = false;
        private string m_remoteAddr = String.Empty;
        private string m_digis = String.Empty;

        private delegate void OnCommandHandler(string[] args);
        private IDictionary<string, OnCommandHandler> m_cmdTbl =
            new Dictionary<string, OnCommandHandler>();

        internal Session(LocalEndpoint localEndpoint, IL3Channel receiver)
            : base(new Dictionary<string, string>(localEndpoint.m_channel.Properties),
                localEndpoint.m_channel.Name + ":" +localEndpoint.Address + "/" +
                receiver.Name, true, L3NullChannel.Instance)
        {
            m_localEndpoint = localEndpoint;
            m_receiver = receiver;
            m_cmdTbl.Add("help", new OnCommandHandler(OnHelp));
            m_cmdTbl.Add("h",    new OnCommandHandler(OnHelp));
            m_cmdTbl.Add("quit", new OnCommandHandler(OnQuit));
            m_cmdTbl.Add("q",    new OnCommandHandler(OnQuit));
            m_cmdTbl.Add("conn", new OnCommandHandler(OnConn));
            m_cmdTbl.Add("c",    new OnCommandHandler(OnConn));
            m_cmdTbl.Add("disc", new OnCommandHandler(OnDisc));
            m_cmdTbl.Add("d",    new OnCommandHandler(OnDisc));
        }

        /// <summary>
        /// Open the interface.
        /// </summary>
        public override void Open()
        {
            base.Open();
        }

        /// <summary>
        /// Close the interface.
        /// </summary>
        public override void Close()
        {
            base.Close();
            try
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_DISCONNECT_Indication");
                m_receiver.Send(new DL_DISCONNECT_Indication());
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.WARNING,
                    m_localEndpoint.Address, "Exception in close (Peer): " + e.Message);
            }

            try
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (dn) : DL_DISCONNECT_Request");
                m_target.Send(new DL_DISCONNECT_Request(), true);
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.WARNING,
                    m_localEndpoint.Address, "Exception in close (Self): " + e.Message);
            }

            try
            {
                if (!L3Channel.IsNullChannel(m_target))
                    m_localEndpoint.m_targetLocalEndpoint.Unbind(m_target);
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.WARNING,
                    m_localEndpoint.Address, "Exception in close (Unbind): " + e.Message);
            }

            m_target = L3NullChannel.Instance;
            m_connected = false;
        }

        /// <summary>
        /// Send a primitive over the channel.
        /// </summary>
        /// <param name="message">The primitive to send.</param>
        /// <param name="expedited">Send expedited if set.</param>
        protected override void Input(IPrimitive _p, bool expedited = false)
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, "RX : " + _p.GetType().Name);
            if (!(_p is DataLinkPrimitive))
                throw new Exception("Expected DataLinkPrimitive. Was: " + _p.GetType().Name);
            DataLinkPrimitive p = (DataLinkPrimitive)_p;
            lock (this)
            {
                switch (p.DataLinkPrimitiveType)
                {
                    case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                        if (m_runtime.LogLevel >= LogLevel.DEBUG)
                            m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_CONNECT_Confirm");
                        m_receiver.Send(new DL_CONNECT_Confirm());
                        if (m_runtime.LogLevel >= LogLevel.DEBUG)
                            m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
                        m_receiver.Send(new DL_UNIT_DATA_Indication(
                            Encoding.UTF8.GetBytes(Greeting(m_receiver.Name))));
                        break;
                    case DataLinkPrimitive_T.DL_CONNECT_Confirm_T:
                    case DataLinkPrimitive_T.DL_CONNECT_Indication_T:
                        m_connected = true;
                        if (m_runtime.LogLevel >= LogLevel.DEBUG)
                            m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
                        m_receiver.Send(new DL_UNIT_DATA_Indication(
                            Encoding.UTF8.GetBytes("\0*** Connected")));
                        break;
                    case DataLinkPrimitive_T.DL_DATA_Indication_T:
                    case DataLinkPrimitive_T.DL_UNIT_DATA_Indication_T:
                        if (m_runtime.LogLevel >= LogLevel.DEBUG)
                            m_runtime.Log(LogLevel.DEBUG, m_name, "TX(up) : " + p.DataLinkPrimitiveTypeName);
                        m_receiver.Send(p);
                        break;
                    case DataLinkPrimitive_T.DL_DATA_Request_T:
                        if (m_connected && (!L3Channel.IsNullChannel(m_target)))
                        {
                            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                                m_runtime.Log(LogLevel.DEBUG, m_name, "TX (dn) : " + p.DataLinkPrimitiveTypeName);
                            m_target.Send(p);
                        }
                        else
                        {
                            ParseCommand(((DL_DATA_Request)p).Data, false);
                        }
                        break;
                    case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T:
                        ParseCommand(((DL_UNIT_DATA_Request)p).Data, false);
                        break;
                    case DataLinkPrimitive_T.DL_DISCONNECT_Confirm_T:
                    case DataLinkPrimitive_T.DL_DISCONNECT_Indication_T:
                        m_connected = false;
                        Unbind();
                        if (m_runtime.LogLevel >= LogLevel.DEBUG)
                            m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
                        m_receiver.Send(new DL_UNIT_DATA_Indication(
                            Encoding.UTF8.GetBytes("\0*** Disconnected" + PROMPT)));
                        break;
                    case DataLinkPrimitive_T.DL_ERROR_Indication_T:
                        if (m_runtime.LogLevel >= LogLevel.DEBUG)
                            m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
                        m_receiver.Send(new DL_UNIT_DATA_Indication(
                            Encoding.UTF8.GetBytes("\0\r\n*** ERROR " +
                                ((DL_ERROR_Indication)p).ErrorCode + " *** : " +
                                ((DL_ERROR_Indication)p).Description + "\r\n")));
                        break;
                    default:
                        m_runtime.Log(LogLevel.WARNING, m_localEndpoint.Address,
                            "Dropping unexpected primitive " + p.DataLinkPrimitiveTypeName);
                        break;
                } // end switch //
            }
        }

        private string Greeting(string name)
        {
            return String.Format("\0Welcome {0} on {1}. Enter 'help' for instructions.{2}",
                name, m_localEndpoint.Address, PROMPT);
        }

        private void ParseCommand(byte[] _cmd, bool isControl)
        {
            if ((_cmd == null) || (_cmd.Length < 2))
                return;
            try
            {
                _cmd[0] = (byte)' '; // PID
                string cmd = Encoding.UTF8.GetString(_cmd).Trim(new char[] { '\r', '\n', '\t', ' ' });
                string[] args = cmd.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length == 0)
                {
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
                    m_receiver.Send(new DL_UNIT_DATA_Indication(
                        Encoding.UTF8.GetBytes(PROMPT)));
                    return;
                }
                OnCommandHandler handler;
                
                m_cmdTbl.TryGetValue(args[0].ToLower(CultureInfo.InvariantCulture), out handler);
                if (handler == null)
                {
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
                    m_receiver.Send(new DL_UNIT_DATA_Indication(
                        Encoding.UTF8.GetBytes(NOT_FOUND + args[0] + PROMPT)));
                    return;
                }
                handler(args);
            }
            catch (Exception e)
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
                m_receiver.Send(new DL_UNIT_DATA_Indication(
                    Encoding.UTF8.GetBytes(ERROR + e.Message + PROMPT)));
            }
        }

        private void OnHelp(string[] args)
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
            m_receiver.Send(new DL_UNIT_DATA_Indication(
                Encoding.UTF8.GetBytes("\0" +
                    "\t\tconn [call] {via} {digis}\tConnect to call (with digis)\r\n" +
                    "\tCommands\r\n" +
                    "\t\thelp\tGet help text\r\n" +
                    "\t\tquit\tQuit session\r\n" +
                    "\t\tdisc\tDisconnect\r\n" +
                    PROMPT
                )));
        }

        private void OnQuit(string[] args)
        {
            Close();
        }

        private void OnConn(string[] args)
        {
            if (m_connected)
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
                m_receiver.Send(new DL_UNIT_DATA_Indication(
                    Encoding.UTF8.GetBytes(
                    "\0*** Already connected to " + m_remoteAddr + " ***\r\n")));
                return;
            }
            // Make address string:
            if (args.Length < 2)
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
                m_receiver.Send(new DL_UNIT_DATA_Indication(
                    Encoding.UTF8.GetBytes(
                    ERROR + " *** Usage: 'connect [call] {via} {digis}' ***" + PROMPT)));
                return;
            }
            try
            {
                string m_remoteAddr = (new L2Callsign(args[1])).ToString();
                StringBuilder sb = new StringBuilder();
                for (int i = 2; i < args.Length; ++i)
                {
                    string arg = args[i].ToLower(CultureInfo.InvariantCulture);
                    if ((i == 2) && ("v".Equals(arg) || "via".Equals(arg)))
                        continue;
                    if (sb.Length > 0)
                        sb.Append(' ');
                    sb.Append(new L2Callsign(arg).ToString());
                } // end for //
                m_digis = sb.ToString();
                if (!L3Channel.IsNullChannel(m_target))
                    Unbind();
                IDictionary<string, string> properties = new Dictionary<string, string>(m_properties);
                if (properties.ContainsKey("RemoteAddr"))
                    properties.Remove("RemoteAddr");
                properties.Add("RemoteAddr", m_remoteAddr);
                if (properties.ContainsKey("Route"))
                    properties.Remove("Route");
                properties.Add("Route", m_digis);
                m_properties.Clear();
                foreach (KeyValuePair<string, string> kvp in properties)
                    m_properties.Add(kvp);
                m_target = m_localEndpoint.m_targetLocalEndpoint.Bind(
                    this, m_properties, m_name + "/" + m_remoteAddr);
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
                m_receiver.Send(new DL_UNIT_DATA_Indication(
                    Encoding.UTF8.GetBytes("\0*** Connecting to " + m_remoteAddr + " ...")));
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (dn) : DL_CONNECT_Request");
                m_target.Send(new DL_CONNECT_Request());
            }
            catch (Exception e)
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
                m_receiver.Send(new DL_UNIT_DATA_Indication(
                    Encoding.UTF8.GetBytes("\0*** Error: " + e.Message + PROMPT)));
            }
        }

        private void OnDisc(string[] args)
        {
            if (!L3Channel.IsNullChannel(m_target))
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (dn) : DL_DISCONNECT_Request");
                m_target.Send(new DL_DISCONNECT_Request());
            }
            Unbind();
        }

        private void Unbind()
        {
            try
            {
                if (!L3Channel.IsNullChannel(m_target))
                    m_localEndpoint.m_targetLocalEndpoint.Unbind(m_target);
            }
            catch (Exception e)
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (up) : DL_UNIT_DATA_Indication");
                m_receiver.Send(new DL_UNIT_DATA_Indication(
                    Encoding.UTF8.GetBytes(
                        "\0*** Error unbinding from " + m_remoteAddr + " via " + m_digis + "\r\n" +
                          "    " + e.Message)));
            }
            finally
            {
                m_target = L3NullChannel.Instance;
                m_connected = false;
                m_remoteAddr = String.Empty;
                m_digis = String.Empty;
                m_properties.Clear();
                foreach (KeyValuePair<string, string> kvp in m_localEndpoint.m_channel.Properties)
                    m_properties.Add(kvp);
            }
        }

        /// <summary>
        /// Local dispose.
        /// </summary>
        /// <param name="intern">Set to <c>true</c> when calling from user code.</param>
        protected override void Dispose(bool intern)
        {
            base.Dispose(intern);
        }

    }
}
