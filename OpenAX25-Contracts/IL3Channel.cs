//
// IL3Channel.cs
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

namespace OpenAX25Contracts
{
    /// <summary>
	/// IL2 channel.
	/// Data channel to transmit and receive transparent frames of octets. The data
	/// in the frames is checked against corruptions on the link layer only. No
	/// interpretion of the channel content is performed.
	/// </summary>
	public interface IL3Channel : IChannel
	{
        /// <summary>
        /// Get or set the target channel.
        /// </summary>
        IL3Channel L3Target { get; set; }

        /// <summary>
        /// Send a primitive over the channel.
        /// </summary>
        /// <param name="sender">The sender of this primitive.</param>
        /// <param name="message">The primitive to send.</param>
        void Send(ILocalEndpoint sender, DataLinkPrimitive message);
    }
}
