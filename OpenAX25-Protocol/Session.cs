using System;
using System.Collections.Generic;
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25_Protocol
{
    internal class Session : L3Channel, IDisposable
    {
        internal readonly Guid m_id;
        internal readonly LocalEndpoint m_localEndpoint;
        internal readonly IL3Channel m_receiver;

        private readonly AX25_Configuration m_config;
        private readonly DataLinkStateMachine m_machine;

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
        ///   <item><term>SRT</term><description>Initial smoothed round trip time in ms [Default: 3000]</description></item>
        ///   <item><term>SAT</term><description>Ínitial smoothed activity timer in ms [Default: 10000]</description></item>
        ///   <item><term>N1</term><description>Ínitial maximum number of octets in the information field of a frame [Default: 255]</description></item>
        ///   <item><term>N2</term><description>Ínitial maximum number of retires permitted [Default: 16]</description></item>
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

            m_machine = new DataLinkStateMachine(m_config);
            m_machine.OnDataLinkOutputEvent += new OnDataLinkOutputEventHandler(OnDataLinkOutput);
            m_machine.OnLinkMultiplexerOutputEvent += new OnLinkMultiplexerOutputEventHandler(OnLinkMultiplexerOutput);
            m_machine.OnAX25OutputEvent += new OnAX25OutputEventHandler(OnAX25Output);
        
        }

        protected override void Input(DataLinkPrimitive p)
        {
            m_machine.Input(p);
        }

        private void OnDataLinkOutput(DataLinkPrimitive p)
        {
            if (m_runtime.LogLevel >= LogLevel.INFO)
                m_runtime.Log(LogLevel.INFO, m_name, "Output " + p.DataLinkPrimitiveTypeName);
            m_receiver.Send(p);
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
