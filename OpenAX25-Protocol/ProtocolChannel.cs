//
// ProtocolChannel.m_sourceCall
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
using System.Linq;
using System.Text;
using OpenAX25Core;
using OpenAX25Contracts;

namespace OpenAX25_Protocol
{
    internal class ProtocolChannel : L2Channel, IL3Channel, IL3DataLinkProvider
    {

        private readonly IDictionary<string, LocalEndpoint> m_localAddresses =
            new Dictionary<string, LocalEndpoint>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="properties">Properties of the channel.
        /// <list type="bullet">
        ///   <listheader><term>Property name</term><description>Description</description></listheader>
        ///   <item><term>Name</term><description>Name of the channel [Mandatory]</description></item>
        ///   <item><term>Target</term><description>
        ///   Where to attach this channel to [Optional, default: ROUTER]</description></item>
        /// </list>
        /// </param>
        internal ProtocolChannel(IDictionary<string, string> properties)
			: base(properties)
		{
		}

        /// <summary>
        /// Get or set the target channel.
        /// </summary>
        public IL3Channel L3Target {
            get {
                return null;
            }
            set {
                throw new Exception("Cannot set target of ProtocolChannel");
            }
        }

        /// <summary>
        /// Send a primitive over the channel.
        /// </summary>
        /// <param name="message">The primitive to send.</param>
        public void Send(DataLinkPrimitive message) {
            throw new Exception("Cannot send primitive on ProtocolChannel");
        }

        /// <summary>
        /// Forward a frame to the channel.
        /// </summary>
        /// <param name="frame">The frame to send</param>
        /// <returns>Frame number that identifies the frame for a later reference.
        /// The number is rather long, however wrap around is not impossible.</returns>
        public override UInt64 ForwardFrame(L2Frame frame)
        {
            return base.ForwardFrame(frame);
        }

        /// <summary>
        /// Here the frames are coming in.
        /// </summary>
        /// <param name="frame">The frame to route.</param>
        protected override void OnForward(L2Frame frame)
        {
            L2Header header = new L2Header(frame.data);
            // Get frame data:
            int iData = 0;
            for (; iData < frame.data.Length; ++iData)
                if ((frame.data[iData] & 0x01) != 0x00)
                    break;
            byte[] data = new Byte[frame.data.Length - iData - 1];
            Array.Copy(frame.data, iData + 1, data, 0, data.Length);
            // Perform routing:
            string targetCall = header.nextHop.ToString();
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
            lock (this)
            {
                foreach (LocalEndpoint lep in m_localAddresses.Values)
                {
                    lep.Dispose();
                } // end foreach //
                m_localAddresses.Clear();
            }
            base.Close();
        }

        /// <summary>
        /// Resets the channel. The data link is closed and reopened. All pending
        /// data is withdrawn.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
        }

        /// <summary>
        /// Attach a new local endpoint.
        /// </summary>
        /// <param name="address">Local address of the endpoint.</param>
        /// <param name="properties">Endpoint properties, optional.</param>
        /// <returns>Local endopoint object that can be used to create sessions.</returns>
        public ILocalEndpoint RegisterLocalEndpoint(string address, IDictionary<string, string> properties)
        {
            lock (this)
            {
                L2Callsign cs = new L2Callsign(address);
                string key = cs.ToString();
                m_runtime.Log(
                    LogLevel.INFO, m_name, "Register local endpoint \"" + key + "\"");
                if (m_localAddresses.ContainsKey(key))
                    throw new DuplicateNameException("Address: \"" + key + "\" already registered");
                LocalEndpoint ep = new LocalEndpoint(this, cs, key, properties);
                m_localAddresses.Add(key, ep);
                return ep;
            }
        }

        /// <summary>
        /// Unattach a local endpoint that where previously registered.
        /// </summary>
        /// <param name="endpoint">The endpoint to unregister.</param>
        public void UnregisterLocalEndpoint(ILocalEndpoint endpoint)
        {
            lock (this)
            {
                m_runtime.Log(
                    LogLevel.INFO, m_name, "Unregister local endpoint \"" + endpoint.Address + "\"");
                m_localAddresses.Remove(((LocalEndpoint)endpoint).m_key);
            }
        }

    }
}
