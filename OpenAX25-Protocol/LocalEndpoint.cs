using System;
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25_Protocol
{
    internal class LocalEndpoint : ILocalEndpoint
    {
        internal readonly Guid id;
        internal readonly L2Callsign cs;
        internal readonly string ky;
        internal readonly IL3Channel ch;

        internal readonly string m_name;
        internal readonly DataLinkStateMachine m_machine;

        private readonly Runtime m_runtime;

        internal LocalEndpoint(L2Callsign callsign, string key, IL3Channel channel, AX25_Configuration config)
        {
            id = Guid.NewGuid();
            cs = callsign;
            ky = key;
            ch = channel;

            m_name = channel.Name + ":" + ky;
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
                return ky;
            }
        }

        /// <summary>
        /// Return the endpoint id.
        /// </summary>
        public Guid Id
        {
            get
            {
                return id;
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
