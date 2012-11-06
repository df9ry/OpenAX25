using OpenAX25Core;
using OpenAX25Contracts;
using System;
using System.Collections.Generic;

namespace OpenAX25_Protocol
{
    internal class DataLinkProviderProxy : L3Channel, IL3DataLinkProvider
    {
        private readonly ProtocolChannel m_channel;
        private readonly IDictionary<Guid, LocalEndpoint> m_registrations = new Dictionary<Guid, LocalEndpoint>();
        private readonly IDictionary<string, LocalEndpoint> m_addresses = new Dictionary<string, LocalEndpoint>();

        internal DataLinkProviderProxy(ProtocolChannel channel)
            : base(GetProperties(channel), true)
        {
            m_channel = channel;
        }

        protected override void Input(DataLinkPrimitive p)
        {
        }

        /// <summary>
        /// Attach an endpoint with an address and request notifications to the
        /// assosiated channel.
        /// </summary>
        /// <param name="address">The address to register the channel for.</param>
        /// <param name="channel">The channel that shall receive the notifications.</param>
        /// <returns>Registration ID that can be used to unregister the channel later.</returns>
        public Guid RegisterL3Endpoint(string address, IL3Channel channel)
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
                LocalEndpoint ep = new LocalEndpoint(cs, ky, channel);
                m_addresses.Add(ky, ep);
                m_registrations.Add(ep.id, ep);
                return ep.id;
            }
        }

        /// <summary>
        /// Unattach an endpoint that where previously registeres fo a channel.
        /// </summary>
        /// <param name="registration">The registration Guid previously registered.</param>
        public void UnregisterL3Endpoint(Guid registration)
        {
            lock (this)
            {
                if (!m_registrations.ContainsKey(registration))
                    throw new NotFoundException(
                        "Endpoint with registration \"" + registration + "\" not found.");
                LocalEndpoint ep = m_registrations[registration];
                m_runtime.Log(
                    LogLevel.INFO, m_name, "Unregister local endpoint \"" + ep.ky + "\"");
                m_registrations.Remove(registration);
                m_addresses.Remove(ep.ky);
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
