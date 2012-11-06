//
// IL3DataLinkProvider.cs
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

namespace OpenAX25Contracts
{
    /// <summary>
    /// A Layer 3 channel that provides a Data Link connection.
    /// </summary>
    public interface IL3DataLinkProvider : IL3Channel
    {
        /// <summary>
        /// Attach an endpoint with an address and request notifications to the
        /// assosiated channel.
        /// </summary>
        /// <param name="address">The address to register the channel for.</param>
        /// <param name="channel">The channel that shall receive the notifications.</param>
        /// <returns>Registration ID that can be used to unregister the channel later.</returns>
        Guid RegisterL3Endpoint(string address, IL3Channel channel);

        /// <summary>
        /// Unattach an endpoint that where previously registeres fo a channel.
        /// </summary>
        /// <param name="registration">The registration Guid previously registered.</param>
        void UnregisterL3Endpoint(Guid registration);
    }
}
