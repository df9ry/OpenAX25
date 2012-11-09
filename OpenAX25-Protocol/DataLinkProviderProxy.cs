using OpenAX25Core;
using OpenAX25Contracts;
using System;
using System.Collections.Generic;

namespace OpenAX25_Protocol
{
    internal class DataLinkProviderProxy : L3Channel, IL3DataLinkProvider
    {
        private readonly ProtocolChannel m_channel;
        private readonly AX25_Configuration m_config;
        private readonly IDictionary<string, LocalEndpoint> m_addresses = new Dictionary<string, LocalEndpoint>();

        internal DataLinkProviderProxy(ProtocolChannel channel)
            : base(GetProperties(channel), true)
        {
            m_channel = channel;
            m_config = new AX25_Configuration(m_name);
            m_config.Initial_N1 = channel.Initial_N1;
            m_config.Initial_N2 = channel.Initial_N2;
            m_config.Initial_SAT = channel.Initial_SAT;
            m_config.Initial_SRT = channel.Initial_SRT;
        }

        protected override void Input(ILocalEndpoint le, DataLinkPrimitive p)
        {
            m_runtime.Log(LogLevel.INFO, m_name, "Received " + p.DataLinkPrimitiveTypeName);
            ((LocalEndpoint)le).m_machine.Input(p);
        }

        /// <summary>
        /// Attach an endpoint with an address and request notifications to the
        /// assosiated channel.
        /// </summary>
        /// <param name="address">The address to register the channel for.</param>
        /// <param name="channel">The channel that shall receive the notifications.</param>
        /// <returns>Local endpoint that can be used for later communication.</returns>
        public ILocalEndpoint RegisterConnection(string address, IL3Channel channel)
        {
            lock (this)
            {
                if (channel == null)
                    throw new ArgumentNullException("channel");
                L2Callsign cs = new L2Callsign(address);
                string ky = cs.ToString();
                m_runtime.Log(
                    LogLevel.INFO, m_name, "Register local endpoint \"" + ky + "\"");
                if (m_addresses.ContainsKey(ky))
                    throw new DuplicateNameException("Address: \"" + ky + "\" already registered");
                LocalEndpoint ep = new LocalEndpoint(cs, ky, channel, m_config);
                m_addresses.Add(ky, ep);
                return ep;
            }
        }

        /// <summary>
        /// Unattach an endpoint that where previously registeres fo a channel.
        /// </summary>
        /// <param name="registration">Local Endpoint to unregister.</param>
        public void UnregisterConnection(ILocalEndpoint ep)
        {
            lock (this)
            {
                m_runtime.Log(
                    LogLevel.INFO, m_name, "Unregister local endpoint \"" + ep.Address + "\"");
                m_addresses.Remove(((LocalEndpoint)ep).ky);
            }
        }

        private static IDictionary<string,string> GetProperties(ProtocolChannel channel)
        {
            IDictionary<string,string> prop = new Dictionary<string,string>();
            prop.Add("Name", channel.Name + "[L3]");
            prop.Add("Target", "L3NULL");
            return prop;
        }

    }
}
