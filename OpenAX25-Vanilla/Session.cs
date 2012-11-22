using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25_Vanilla
{
    internal sealed class Session : IL3Channel, IDisposable
    {
        private const string PROMPT = "\0\r\n> ";
        private const string ERROR = "\0Error: ";
        private const string NOT_FOUND = "\0Not found: ";
        private readonly Runtime m_runtime = Runtime.Instance;
        private readonly LocalEndpoint m_localEndpoint;
        private readonly IL3Channel m_receiver;
        private readonly string m_name;
        private IL3Channel m_target = null;
        private bool m_connected = false;
        private string m_remoteAddr = "";

        private delegate void OnCommandHandler(string[] args);
        private IDictionary<string, OnCommandHandler> m_cmdTbl =
            new Dictionary<string, OnCommandHandler>();

        internal Session(LocalEndpoint localEndpoint, IL3Channel receiver)
        {
            m_localEndpoint = localEndpoint;
            m_receiver = receiver;
            m_name = localEndpoint.Address + "(" + receiver.Name + ")";
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
        /// Gets the name of the channel. This name have to be unique accross the
        /// application and can never change. There is no interpretion or syntax check
        /// performed.
        /// </summary>
        /// <value>
        /// The unique name of this channel.
        /// </value>
        public string Name { get { return m_name; } }

        /// <summary>
        /// Gets the properties of this channel.
        /// </summary>
        /// <value>
        /// The properties.
        /// </value>
        public IDictionary<string, string> Properties { get { return m_target.Properties; } }

        /// <summary>
        /// Open the interface.
        /// </summary>
        public void Open()
        {
            if (m_target != null)
                return;
            m_target = m_localEndpoint.m_targetLocalEndpoint.Bind(
                this, m_localEndpoint.m_channel.Properties);
            if (m_target == null)
                throw new Exception("Vanilla: Unable to bind");
        }

        /// <summary>
        /// Close the interface.
        /// </summary>
        public void Close()
        {
            try
            {
                m_receiver.Send(new DL_DISCONNECT_Indication());
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.WARNING,
                    m_localEndpoint.Address, "Exception in close (Peer): " + e.Message);
            }

            try
            {
                if (m_target != null)
                    m_target.Send(new DL_DISCONNECT_Request(), true);
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.WARNING,
                    m_localEndpoint.Address, "Exception in close (Self): " + e.Message);
            }

            try
            {
                if (m_target != null)
                    m_localEndpoint.m_targetLocalEndpoint.Unbind(m_target);
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.WARNING,
                    m_localEndpoint.Address, "Exception in close (Unbind): " + e.Message);
            }

            m_target = null;
            m_connected = false;
        }

        /// <summary>
        /// Resets the channel. The data link is closed and reopened. All pending
        /// data is withdrawn.
        /// </summary>
        public void Reset()
        {
        }

        /// <summary>
        /// Get or set the target channel.
        /// </summary>
        public IL3Channel L3Target {
            get { return m_localEndpoint.m_channel.L3Target; }
            set { }
        }

        /// <summary>
        /// Send a primitive over the channel.
        /// </summary>
        /// <param name="message">The primitive to send.</param>
        /// <param name="expedited">Send expedited if set.</param>
        public void Send(DataLinkPrimitive message, bool expedited = false)
        {
            switch (message.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_CONNECT_Request_T :
                    m_receiver.Send(new DL_CONNECT_Confirm());
                    m_receiver.Send(new DL_UNIT_DATA_Indication(
                        Encoding.UTF8.GetBytes(Greeting(m_receiver.Name))));
                    break;
                case DataLinkPrimitive_T.DL_CONNECT_Confirm_T:
                case DataLinkPrimitive_T.DL_CONNECT_Indication_T:
                    m_connected = true;
                    m_receiver.Send(new DL_UNIT_DATA_Indication(
                        Encoding.UTF8.GetBytes("\0\r\n***Connected***\r\n")));
                    break;
                case DataLinkPrimitive_T.DL_DATA_Indication_T :
                case DataLinkPrimitive_T.DL_UNIT_DATA_Indication_T :
                    m_receiver.Send(message);
                    break;
                case DataLinkPrimitive_T.DL_DATA_Request_T:
                    if (m_connected)
                        m_target.Send(message);
                    else
                        ParseCommand(((DL_DATA_Request)message).Data, false);
                    break;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T:
                    ParseCommand(((DL_UNIT_DATA_Request)message).Data, false);
                    break;
                case DataLinkPrimitive_T.DL_DISCONNECT_Confirm_T :
                case DataLinkPrimitive_T.DL_DISCONNECT_Indication_T :
                    m_connected = false;
                    m_receiver.Send(new DL_UNIT_DATA_Indication(
                        Encoding.UTF8.GetBytes("\0\r\n***Disconnected***" + PROMPT)));
                    break;
                case DataLinkPrimitive_T.DL_ERROR_Indication_T :
                    m_receiver.Send(new DL_UNIT_DATA_Indication(
                        Encoding.UTF8.GetBytes("\0\r\n*** ERROR " +
                            ((DL_ERROR_Indication)message).ErrorCode + " *** : " +
                            ((DL_ERROR_Indication)message).Description + "\r\n")));
                    break;
                default:
                    m_runtime.Log(LogLevel.WARNING, m_localEndpoint.Address,
                        "Dropping unexpected primitive " + message.DataLinkPrimitiveTypeName);
                    break;
            } // end switch //
        }

        /// <summary>
        /// Dispose this object, when it is not longer needed.
        /// </summary>
        public void Dispose()
        {
            //Close();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private string Greeting(string name)
        {
            return String.Format("\0Welcome {0} on {1}. Enter 'help' for instructions.{2}",
                name, m_localEndpoint.Address, PROMPT);
        }

        private void ParseCommand(byte[] _cmd, bool isControl)
        {
            try
            {
                string cmd = Encoding.UTF8.GetString(_cmd).Trim(new char[] { '\r', '\n', '\t', ' ' });
                string[] args = cmd.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length == 0)
                {
                    m_receiver.Send(new DL_UNIT_DATA_Indication(
                        Encoding.UTF8.GetBytes(PROMPT)));
                    return;
                }
                OnCommandHandler handler;
                
                m_cmdTbl.TryGetValue(args[0].ToLower(CultureInfo.InvariantCulture), out handler);
                if (handler == null)
                {
                    m_receiver.Send(new DL_UNIT_DATA_Indication(
                        Encoding.UTF8.GetBytes(NOT_FOUND + args[0] + PROMPT)));
                    return;
                }
                handler(args);
            }
            catch (Exception e)
            {
                m_receiver.Send(new DL_UNIT_DATA_Indication(
                    Encoding.UTF8.GetBytes(ERROR + e.Message + PROMPT)));
            }
        }

        private void OnHelp(string[] args)
        {
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
                m_receiver.Send(new DL_UNIT_DATA_Indication(
                    Encoding.UTF8.GetBytes(
                    "\0***Already connected to " + m_remoteAddr + " ***\r\n")));
                return;
            }
            // Make address string:
            if (args.Length < 2)
            {
                m_receiver.Send(new DL_UNIT_DATA_Indication(
                    Encoding.UTF8.GetBytes(
                    ERROR + " *** Usage: 'connect [call] {via} {digis}' ***" + PROMPT)));
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(args[1]); // Destination
            for (int i = 2; i < args.Length; ++i)
            {
                string arg = args[i].ToLower(CultureInfo.InvariantCulture);
                if ((i == 2) && ("v".Equals(arg) || "via".Equals(arg)))
                    continue;
                sb.Append(' ');
                sb.Append(arg);
            } // end for //
            m_remoteAddr = sb.ToString();
            m_receiver.Send(new DL_UNIT_DATA_Indication(
                Encoding.UTF8.GetBytes("\0*** Connecting to " + m_remoteAddr + " ***\r\n")));
            m_target.Send(new DL_CONNECT_Request(m_remoteAddr, AX25Version.V2_0));
        }

        private void OnDisc(string[] args)
        {
            m_connected = false;
            m_remoteAddr = "";
            m_target.Send(new DL_DISCONNECT_Request());
        }

        /// <summary>
        /// Local dispose.
        /// </summary>
        /// <param name="intern">Set to <c>true</c> when calling from user code.</param>
        private void Dispose(bool intern)
        {
        }

    }
}
