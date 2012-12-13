//
// VanillaChannel.cs
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

namespace OpenAX25_Vanilla
{
    public sealed class VanillaChannel : Channel, IL3DataLinkProvider, IDisposable
    {

        private readonly IL3DataLinkProvider m_target;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="properties">Properties of the channel.
        /// <list type="bullet">
        ///   <listheader><term>Property name</term><description>Description</description></listheader>
        ///   <item><term>Name</term><description>Name of the channel [Mandatory]</description></item>
        ///   <item><term>Target</term><description>
        ///   Where to attach this channel to [Optional, default: PROTO]</description></item>
        /// </list>
        /// </param>
        internal VanillaChannel(IDictionary<string, string> properties)
            : base(properties)
		{
            m_runtime = Runtime.Instance;
            string _target;
            if (!properties.TryGetValue("Target", out _target))
                _target = "PROTO";
            IChannel target  = m_runtime.LookupChannel(_target);
            if (target == null)
                throw new InvalidPropertyException("Target not found: " + _target);
            if (!(target is IL3DataLinkProvider))
                throw new InvalidPropertyException("Target not datalink provider: " + _target);
            m_target = (IL3DataLinkProvider)target;
        }

        /// <summary>
        /// Attach a new local endpoint.
        /// </summary>
        /// <param name="address">Local address of the endpoint.</param>
        /// <param name="properties">Endpoint properties, optional.</param>
        /// <returns>Local endopoint object that can be used to create sessions.</returns>
        public ILocalEndpoint RegisterLocalEndpoint(string address, IDictionary<string, string> properties = null)
        {
            ILocalEndpoint targetLocalEndpoint = m_target.RegisterLocalEndpoint(address, properties);
            LocalEndpoint proxyLocalEndpoint = new LocalEndpoint(this, targetLocalEndpoint);
            return proxyLocalEndpoint;
        }

        /// <summary>
        /// Unattach a local endpoint that where previously registered.
        /// </summary>
        /// <param name="endpoint">The endpoint to unregister.</param>
        public void UnregisterLocalEndpoint(ILocalEndpoint endpoint)
        {
            m_target.UnregisterLocalEndpoint(endpoint);            
        }

        /// <summary>
        /// Dispose this object, when it is not longer needed.
        /// </summary>
        public void Dispose()
        {
            Close();
            Dispose(true);
            GC.SuppressFinalize(this);
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
