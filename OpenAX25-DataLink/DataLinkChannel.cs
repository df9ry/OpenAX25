//
// DataLinkChannel.cs
// 
//  Author:
//       Tania Knoebl (DF9RY) DF9RY@DARC.de
//  
//  Copyright © 2012 Tania Knoebl (DF9RY)
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>
//

using System;
using System.Collections.Generic;
using OpenAX25Contracts;
using OpenAX25Core;

/// <summary>
/// This class simulates a duplex physical state machine on top of a
/// L2Channel.
/// </summary>
namespace OpenAX25_DataLink
{
    public sealed class DataLinkChannel : IL3DataLinkProvider
    {
        internal readonly Runtime m_runtime;
        internal readonly IDictionary<string, string> m_properties;
        internal readonly string m_name;
        internal readonly IL3Channel m_target;
        internal readonly IL3DataLinkProvider m_dataLinkProvider;

        private bool m_isOpen = false;
        private IDictionary<L2Callsign, ClientEndpoint> m_clientEndpoints =
            new Dictionary<L2Callsign, ClientEndpoint>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="properties">Properties of the channel.
        /// <list type="bullet">
        ///   <listheader><term>Property name</term><description>Description</description></listheader>
        ///   <item><term>Name</term><description>Name of the channel [Default: DL]</description></item>
        ///   <item><term>Target</term><description>Where to route packages to [Default: LMPX]</description></item>
        /// </list>
        /// </param>
        public DataLinkChannel(IDictionary<string, string> properties)
        {
            m_runtime = Runtime.Instance;
            m_properties = (properties == null) ?
                new Dictionary<string, string>() : new Dictionary<string, string>(properties);
            string _val;
            if (!m_properties.TryGetValue("Name", out _val))
                _val = "DL";
            m_name = _val;
            if (!m_properties.TryGetValue("Target", out _val))
                _val = "LMPX";
            IChannel target = m_runtime.LookupChannel(_val);
            if (target == null)
                throw new Exception("target not found: " + _val);
            if (!(target is IL3Channel))
                throw new Exception("target is not a layer3 channel: " + _val);
            m_target = (IL3Channel)target;
            if (!(target is IL3DataLinkProvider))
                throw new Exception("target is not a datalink provider: " + _val);
            m_dataLinkProvider = (IL3DataLinkProvider)target;
            m_runtime.RegisterChannel(this);
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
        public IDictionary<string, string> Properties { get { return m_properties; } }

        /// <summary>
        /// Open the interface.
        /// </summary>
        public void Open()
        {
            lock (this)
            {
                if (m_isOpen)
                    return;
                m_runtime.Log(LogLevel.INFO, m_name, "Open Channel");
                m_isOpen = true;
            } // end lock //
        }

        /// <summary>
        /// Close the interface.
        /// </summary>
        public void Close()
        {
            lock (this)
            {
                if (!m_isOpen)
                    return;
                m_runtime.Log(LogLevel.INFO, m_name, "Close Channel");
                m_isOpen = false;
            } // end lock //
        }

        /// <summary>
        /// Resets the channel. The data link is closed and reopened. All pending
        /// data is withdrawn.
        /// </summary>
        public void Reset()
        {
            lock (this)
            {
                Close();
                Open();
            } // end lock //
        }

        /// <summary>
        /// Attach a new local endpoint.
        /// </summary>
        /// <param name="address">Local address of the endpoint.</param>
        /// <param name="properties">Endpoint properties, optional.</param>
        /// <returns>Local endopoint object that can be used to create sessions.</returns>
        public ILocalEndpoint RegisterLocalEndpoint(string address, IDictionary<string, string> properties)
        {
            L2Callsign callsign = new L2Callsign(address);
            lock (this)
            {
                // Normalize address:
                if (m_clientEndpoints.ContainsKey(callsign))
                    throw new DuplicateNameException(callsign.ToString());
                ClientEndpoint ep = new ClientEndpoint(this, callsign);
                ep.Open();
                m_clientEndpoints.Add(callsign, ep);
                return ep;
            } // end lock //
        }

        /// <summary>
        /// Unattach a local endpoint that where previously registered.
        /// </summary>
        /// <param name="endpoint">The endpoint to unregister.</param>
        public void UnregisterLocalEndpoint(ILocalEndpoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException("endpoint");
            if (!(endpoint is ClientEndpoint))
                throw new Exception(String.Format("Invalid class {0}. Expected {1}.",
                    endpoint.GetType().Name, typeof(ClientEndpoint).Name));
            ClientEndpoint ep = (ClientEndpoint)endpoint;
            L2Callsign key = ep.m_callsign;
            lock (this)
            {
                if (!m_clientEndpoints.ContainsKey(key))
                    throw new NotFoundException(key.ToString());
                m_clientEndpoints.Remove(key);
                ep.Close();
            } // end lock //
        }

    } // end class //
}
