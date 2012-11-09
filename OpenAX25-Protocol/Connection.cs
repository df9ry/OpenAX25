using System;
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25_Protocol
{
    internal class Connection : IConnection
    {
        internal readonly Guid m_id;
        internal readonly L2Callsign m_sourceCall;
        internal readonly L2Callsign[] m_targetPath;
        internal readonly string m_key;
        internal readonly string m_name;
        internal readonly DataLinkStateMachine m_machine;

        private readonly Runtime m_runtime;

        internal Connection(L2Callsign sourceCall, L2Callsign[] targetPath, string key,
            AX25_Configuration config)
        {
            if (targetPath == null)
                throw new ArgumentNullException("targetPath");
            if (targetPath.Length == 0)
                throw new ArgumentOutOfRangeException("targetPath.Length == 0");
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");
            m_id = Guid.NewGuid();
            m_sourceCall = sourceCall;
            m_key = key;
            m_name = "AX.25 connection: " + key;
            m_runtime = Runtime.Instance;

            m_machine = new DataLinkStateMachine(config);
            m_machine.OnDataLinkOutputEvent += new OnDataLinkOutputEventHandler(OnDataLinkOutput);
            m_machine.OnLinkMultiplexerOutputEvent += new OnLinkMultiplexerOutputEventHandler(OnLinkMultiplexerOutput);
            m_machine.OnAX25OutputEvent += new OnAX25OutputEventHandler(OnAX25Output);
        }

        /// <summary>
        /// Return the endpoint address.
        /// </summary>
        public string Address
        {
            get
            {
                return m_key;
            }
        }

        /// <summary>
        /// Return the endpoint m_id.
        /// </summary>
        public Guid Id
        {
            get
            {
                return m_id;
            }
        }

        private void OnDataLinkOutput(DataLinkPrimitive p)
        {
            if (m_runtime.LogLevel >= LogLevel.INFO)
                m_runtime.Log(LogLevel.INFO, m_name, "Output " + p.DataLinkPrimitiveTypeName);
            ch.Send(this, p);
        }

        private void OnLinkMultiplexerOutput(LinkMultiplexerPrimitive p)
        {
            if (m_runtime.LogLevel >= LogLevel.INFO)
                m_runtime.Log(LogLevel.INFO, m_name, "Output " + p.LinkMultiplexerPrimitiveTypeName);
        }

        private void OnAX25Output(AX25Frame f)
        {
            if (m_runtime.LogLevel >= LogLevel.INFO)
                m_runtime.Log(LogLevel.INFO, m_name, "Output " + f.ToString());
        }
    }
}
