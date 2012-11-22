using System;
using System.Collections.Generic;
using OpenAX25Contracts;
using OpenAX25Core;
using System.Text;

namespace OpenAX25_Protocol
{
    internal class Session : L3Channel, IDisposable
    {
        internal readonly Guid m_id;
        internal readonly LocalEndpoint m_localEndpoint;
        internal readonly IL3Channel m_receiver;

        private readonly AX25_Configuration m_config;
        private readonly DataLinkStateMachine m_machine;

        private L2Callsign m_defaultTargetCall;
        private L2Callsign m_targetCall;
        private L2Callsign[] m_defaultDigis;
        private L2Callsign[] m_digis;

        /** Management parameters */
        //private readonly int NM201; // Maximum number of retries of the XID command.

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="localEndpoint">The local endpoint of this session.</param>
        /// <param name="receiver">The receiver channel.</param>
        /// <param name="properties">Properties of the channel.
        /// <list type="bullet">
        ///   <listheader><term>Property name</term><description>Description</description></listheader>
        ///   <item><term>Name</term><description>Name of the channel [Mandatory]</description></item>
        ///   <item><term>AX25Version</term><description>AX.25 Version to use (2.0|2.2) [Default: 2.0]</description></item>
        ///   <item><term>SRT</term><description>Initial smoothed round trip time in ms [Default: 3000]</description></item>
        ///   <item><term>SAT</term><description>Ínitial smoothed activity timer in ms [Default: 10000]</description></item>
        ///   <item><term>N1</term><description>Ínitial maximum number of octets in the information field of a frame [Default: 255]</description></item>
        ///   <item><term>N2</term><description>Ínitial maximum number of retires permitted [Default: 16]</description></item>
        ///   <item><term>DefaultTarget</term><description>Default target callsign [Default: 'QST']</description></item>
        ///   <item><term>DefaultDigis</term><description>Default digis [Default: '']</description></item>
        /// </list>
        /// </param>
        internal Session(LocalEndpoint localEndpoint, IL3Channel receiver,
            IDictionary<string,string> properties, string alias)
            : base(properties, alias)
        {
            m_id = Guid.NewGuid();
            if (localEndpoint == null)
                throw new ArgumentNullException("localEndpoint");
            m_localEndpoint = localEndpoint;
            if (receiver == null)
                throw new ArgumentNullException("receiver");
            m_receiver = receiver;

            m_config = new AX25_Configuration();

            string _v;

            if (!properties.TryGetValue("AX25Version", out _v))
                _v = "2.0";
            try
            {
                if ("2.0".Equals(_v))
                    m_config.Initial_version = AX25Version.V2_0;
                else if ("2.2".Equals(_v))
                    m_config.Initial_version = AX25Version.V2_2;
                else
                    throw new ArgumentOutOfRangeException("2.0|2.2");
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("AX25Version", ex);
            }

            if (!properties.TryGetValue("SRT", out _v))
                _v = "3000";
            try
            {
                m_config.Initial_SRT = Int32.Parse(_v);
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("SRT", ex);
            }

            if (!properties.TryGetValue("SAT", out _v))
                _v = "10000";
            try
            {
                m_config.Initial_SAT = Int32.Parse(_v);
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("SAT", ex);
            }

            if (!properties.TryGetValue("N1", out _v))
                _v = "255";
            try
            {
                m_config.Initial_N1 = Int32.Parse(_v);
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("N1", ex);
            }

            if (!properties.TryGetValue("N2", out _v))
                _v = "16";
            try
            {
                m_config.Initial_N2 = Int32.Parse(_v);
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("N2", ex);
            }

            if (!properties.TryGetValue("DefaultTarget", out _v))
                _v = "QST";
            try
            {
                m_defaultTargetCall = new L2Callsign(_v);
                m_targetCall = m_defaultTargetCall;
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("DefaultTarget", ex);
            }

            if (!properties.TryGetValue("DefaultDigis", out _v))
                _v = "";
            try
            {
                string[] _digis = _v.Split(new char[] { ' ', ',' },
                    StringSplitOptions.RemoveEmptyEntries);
                m_defaultDigis = new L2Callsign[_digis.Length];
                for (int i = 0; i < _digis.Length; ++i)
                    m_defaultDigis[i] = new L2Callsign(_digis[i]);
                m_digis = m_defaultDigis;
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("DefaultDigis", ex);
            }

            m_machine = new DataLinkStateMachine(m_config);
            m_machine.OnDataLinkOutputEvent += new OnDataLinkOutputEventHandler(OnDataLinkOutput);
            m_machine.OnLinkMultiplexerOutputEvent += new OnLinkMultiplexerOutputEventHandler(OnLinkMultiplexerOutput);
            m_machine.OnAX25OutputEvent += new OnAX25OutputEventHandler(OnAX25Output);
            m_machine.OnAX25OutputExpeditedEvent += new OnAX25OutputEventHandler(OnAX25ExpeditedOutput);
        
        }

        public override void Close()
        {
            m_machine.Input(new DL_DISCONNECT_Request());
            base.Close();
        }

        internal bool CanAccept(L2Callsign source, L2Callsign[] digis)
        {
            if (digis == null)
                throw new ArgumentNullException("digis");
            if (m_targetCall.Equals(L2Callsign.CQ))
                return true;
            if (!m_targetCall.Equals(source))
                return false;
            int l = m_digis.Length;
            if (l != digis.Length)
                return false;
            for (int i = 0; i < l; ++i)
            {
                if (!m_digis[i].Equals(digis[l - i - 1]))
                    return false;
            } // end for //
            return true;
        }

        protected override void Input(DataLinkPrimitive p, bool expedited)
        {
            if (p.DataLinkPrimitiveType == DataLinkPrimitive_T.DL_CONNECT_Request_T)
            {
                DL_CONNECT_Request rq = (DL_CONNECT_Request)p;
                string[] _path = rq.RemoteAddr.Split(new char[] { ' ', ',' },
                    StringSplitOptions.RemoveEmptyEntries);
                if (_path.Length == 0)
                {
                    m_targetCall = m_defaultTargetCall;
                    m_digis = m_defaultDigis;
                }
                else
                {
                    m_targetCall = new L2Callsign(_path[0]);
                    m_digis = new L2Callsign[_path.Length - 1];
                    for (int i = 0; i < _path.Length - 2; ++i)
                        m_digis[i] = new L2Callsign(_path[i + 1]);
                }
            }

            string msg = String.Format("=SEND {0} [{1}]", m_name, p.DataLinkPrimitiveTypeName);
            m_runtime.Monitor(msg);
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, msg);
            
            m_machine.Input(p);
            if (p.DataLinkPrimitiveType == DataLinkPrimitive_T.DL_DISCONNECT_Confirm_T)
            {
                m_targetCall = m_defaultTargetCall;
                m_digis = m_defaultDigis;
            }
        }

        private void OnDataLinkOutput(DataLinkPrimitive p)
        {
            if (p.DataLinkPrimitiveType == DataLinkPrimitive_T.DL_CONNECT_Indication_T)
            {
                DL_CONNECT_Indication rq = (DL_CONNECT_Indication)p;
                string[] _path = rq.RemoteAddr.Split(new char[] { ' ', ',' },
                    StringSplitOptions.RemoveEmptyEntries);
                if (_path.Length == 0)
                {
                    m_targetCall = m_defaultTargetCall;
                    m_digis = m_defaultDigis;
                }
                else
                {
                    m_targetCall = new L2Callsign(_path[0]);
                    m_digis = new L2Callsign[_path.Length - 1];
                    for (int i = 0; i < _path.Length - 2; ++i)
                        m_digis[i] = new L2Callsign(_path[i + 1]);
                }
            }

            string msg = String.Format("=RECV {0} [{1}]", m_name, p.DataLinkPrimitiveTypeName);
            m_runtime.Monitor(msg);
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, msg);

            m_receiver.Send(p);
            if (p.DataLinkPrimitiveType == DataLinkPrimitive_T.DL_DISCONNECT_Confirm_T)
            {
                m_targetCall = m_defaultTargetCall;
                m_digis = m_defaultDigis;
            }
        }

        private void OnLinkMultiplexerOutput(LinkMultiplexerPrimitive p)
        {
            if (m_runtime.LogLevel >= LogLevel.INFO)
                m_runtime.Log(LogLevel.INFO, m_name, "Output " + p.LinkMultiplexerPrimitiveTypeName);
        }

        private void OnAX25ExpeditedOutput(AX25Frame f)
        {
            _OnAX25Output(f, true);
        }

        private void OnAX25Output(AX25Frame f)
        {
            _OnAX25Output(f, false);
        }

        private void _OnAX25Output(AX25Frame f, bool expedited)
        {
            byte[] payload = f.Octets;
            L2Callsign sourceCall = new L2Callsign(m_localEndpoint.m_localCallsign, f.Response);
            L2Callsign targetCall = new L2Callsign(m_targetCall, f.Command);

            string msg = String.Format("*TX [{5:HH:mm:ss.fff}] {0} {1}->{2}{3} [{4}]", m_name,
                sourceCall.ToString(), targetCall.ToString(), DigisToString(m_digis), f.ToString(),
                DateTime.UtcNow);
            m_runtime.Monitor(msg);
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, msg);
            
            L2Header _header = new L2Header(sourceCall, targetCall, m_digis);
            byte[] header = _header.Octets;
            int lHeader = header.Length;
            int lPayload = payload.Length;
            int lFrame = lHeader + lPayload;
            byte[] _frame = new byte[lFrame];
            Array.Copy(header, 0, _frame, 0, lHeader);
            Array.Copy(payload, 0, _frame, lHeader, lPayload);
            L2Frame frame = new L2Frame(m_runtime.NewFrameNo(), false, _frame);
            m_localEndpoint.m_protoChannel.Send(frame/* TODO , expedited*/);
        }

        internal void OnForward(L2Frame f, L2Header header)
        {
            m_targetCall = header.source;
            m_digis = ReverseDigis(header.digis);

            byte[] _data = f.data;
            int l_data = _data.Length;
            int i_payload = 14 + ( m_digis.Length * 7 );
            int l_payload = l_data - i_payload;
            byte[] payload = new byte[l_data - i_payload];
            Array.Copy(_data, i_payload, payload, 0, l_payload); 

            AX25Frame frame = AX25Frame.Create(
                payload, header.isCommand, header.isResponse, m_machine.m_modulo);

            string msg = String.Format("*RX [{5:HH:mm:ss.fff}] {0} {1}<-{2}{3} [{4}]", m_name,
                m_localEndpoint.m_localCallsign.ToString(), m_targetCall.ToString(),
                DigisToString(m_digis), frame.ToString(), DateTime.UtcNow);
            m_runtime.Monitor(msg);
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, msg);

            m_machine.Input(frame);
        }

        private static string DigisToString(L2Callsign[] digis)
        {
            if (digis == null)
                return "<NULL>";
            int l = digis.Length;
            if (l == 0)
                return "";
            StringBuilder sb = new StringBuilder();
            sb.Append(" via");
            foreach (L2Callsign cs in digis)
            {
                sb.Append(' ');
                sb.Append(cs.ToString());
            }
            return sb.ToString();
        }

        private static L2Callsign[] ReverseDigis(L2Callsign[] digis)
        {
            int l = digis.Length;
            L2Callsign[] reverse = new L2Callsign[l];
            for (int i = 0; i < l; ++i)
                reverse[i] = digis[l - i - 1];
            return reverse;
        }
    }

}
