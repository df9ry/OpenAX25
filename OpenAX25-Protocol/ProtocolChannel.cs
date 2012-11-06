//
// ProtocolChannel.cs
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
    class ProtocolChannel : L2Channel, IL3DataLinkProvider
    {

        /** Management parameters */
        private readonly int NM201; // Maximum number of retries of the XID command.
        /** Data Link parameters */
        private readonly int  Initial_SRT;   // Initial smoothed round trip time
        private readonly int  Initial_SAT;   // Ínitial smoothed activity timer
        private readonly long Initial_T1V;   // Initial T1 timer
        private readonly long Initial_T2V;   // Initial T2 timer
        private readonly long Initial_T3V;   // Initial T3 timer
        private readonly int  Initial_N1;    // Initial N1 - Maximum number of octets in the information field of a frame.
        private readonly int  Initial_N2;    // Initial N2 - Maximum number of retires permitted.
        private readonly DataLinkProviderProxy m_dataLinkProviderProxy;

        private IL3Channel m_l3target;

        /// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="properties">Properties of the channel.
		/// <list type="bullet">
		///   <listheader><term>Property name</term><description>Description</description></listheader>
		///   <item><term>Name</term><description>Name of the channel [Mandatory]</description></item>
		///   <item><term>Target</term><description>Where to route packages to [Default: ROUTER]</description></item>
        ///   <item><term>L3Target</term><description>Where to route L3 data to [Default: PROTO]</description></item>
        ///   <item><term>SRT</term><description>Initial smoothed round trip time in ms [Default: 3000]</description></item>
        ///   <item><term>SAT</term><description>Ínitial smoothed activity timer in ms [Default: 10000]</description></item>
        ///   <item><term>T1</term><description>Ínitial T1 timer in ms [Default: 3000]</description></item>
        ///   <item><term>T2</term><description>Ínitial T2 timer in ms [Default: 3000]</description></item>
        ///   <item><term>T3</term><description>Ínitial T3 timer in ms [Default: 30000]</description></item>
        ///   <item><term>N1</term><description>Ínitial maximum number of octets in the information field of a frame [Default: 255]</description></item>
        ///   <item><term>N1</term><description>Ínitial maximum number of retires permitted [Default: 16]</description></item>
        /// </list>
		/// </param>
		public ProtocolChannel(IDictionary<string,string> properties)
			: base(properties)
		{
            string _v;

            if (!properties.TryGetValue("SRT", out _v))
                _v = "3000";
            try
            {
                this.Initial_SRT= Int32.Parse(_v);
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("SRT", ex);
            }

            if (!properties.TryGetValue("SAT", out _v))
                _v = "10000";
            try
            {
                this.Initial_SAT = Int32.Parse(_v);
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("SAT", ex);
            }

            if (!properties.TryGetValue("T1", out _v))
                _v = "3000";
            try
            {
                this.Initial_T1V = Int64.Parse(_v);
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("T1", ex);
            }

            if (!properties.TryGetValue("T2", out _v))
                _v = "3000";
            try
            {
                this.Initial_T2V = Int64.Parse(_v);
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("T2", ex);
            }

            if (!properties.TryGetValue("T3", out _v))
                _v = "30000";
            try
            {
                this.Initial_T3V = Int64.Parse(_v);
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("T3", ex);
            }

            if (!properties.TryGetValue("N1", out _v))
                _v = "255";
            try
            {
                this.Initial_N1 = Int32.Parse(_v);
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("N1", ex);
            }

            if (!properties.TryGetValue("N2", out _v))
                _v = "10";
            try
            {
                this.Initial_N2 = Int32.Parse(_v);
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("N2", ex);
            }

            string _target;
            if (!properties.TryGetValue("L3Target", out _target))
                _target = "L3NULL";
            m_l3target = m_runtime.LookupL3Channel(_target);
            if (m_target == null)
                throw new InvalidPropertyException("L3Target not found: " + _target);

            m_dataLinkProviderProxy = new DataLinkProviderProxy(this);
		}

        /// <summary>
        /// Get or set the default target channel.
        /// </summary>
        public virtual IL3Channel L3Target
        {
            get
            {
                return this.m_l3target;
            }
            set
            {
                this.m_l3target = value;
            }
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
            m_dataLinkProviderProxy.Open();
        }

        /// <summary>
        /// Close the interface.
        /// </summary>
        public override void Close()
        {
            base.Close();
            m_dataLinkProviderProxy.Close();
        }

        /// <summary>
        /// Resets the channel. The data link is closed and reopened. All pending
        /// data is withdrawn.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            m_dataLinkProviderProxy.Reset();
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
            return m_dataLinkProviderProxy.RegisterL3Endpoint(address, channel);
        }

        /// <summary>
        /// Unattach an endpoint that where previously registeres fo a channel.
        /// </summary>
        /// <param name="registration">The registration Guid previously registered.</param>
        public void UnregisterL3Endpoint(Guid registration)
        {
            m_dataLinkProviderProxy.UnregisterL3Endpoint(registration);
        }

                /// <summary>
        /// Send a Data Link primitive to the serving object.
        /// </summary>
        /// <param name="p">Data Link primitive to send.</param>
        public virtual void Send(DataLinkPrimitive p)
        {
            m_dataLinkProviderProxy.Send(p);
        }

    }
}
