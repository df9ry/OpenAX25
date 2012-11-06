//
// IL2Channel.cs
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

namespace OpenAX25Contracts
{
	/// <summary>
	/// IL2Channel.
	/// Data channel to transmit and receive transparent frames of octets. The data
	/// in the frames is checked against corruptions on the link layer only. No
	/// interpretion of the channel content is performed.
	/// </summary>
	public interface IL2Channel : IChannel
	{
        /// <summary>
        /// Get or set the target channel for received data.
        /// </summary>
        IL2Channel L2Target { get; set; }

        /// <summary>
		/// Gets the number of frames available in the rx queue.
		/// </summary>
		/// <value>
		/// The number of frames available in the rx queue.
		/// </value>
		Int32 RXSize { get; }

		/// <summary>
		/// Gets the total number of octets available in the rx queue.
		/// </summary>
		/// <value>
		/// The total number of octets available in the rx queue.
		/// </value>
		Int64 RXOctets { get; }

		/// <summary>
		/// Gets or sets the total number of octets received. Overflow of this
		/// counter will be harmless by cycles the number around.
		/// </summary>
		/// <value>
		/// The rx total number of octets.
		/// </value>
		Int64 RXTotal { get; set; }

		/// <summary>
		/// Gets or sets the number of rx errors occurred.
		/// </summary>
		/// <value>
		/// The number of rx errors occurred.
		/// </value>
		Int64 RXErrors { get; set; }

		/// <summary>
		/// Gets the number of frames waiting in the tx queue.
		/// </summary>
		/// <value>
		/// The number of frames waiting in the tx queue.
		/// </value>
		Int32 TXSize { get; }

		/// <summary>
		/// Gets the total number of octets waiting in the tx queue.
		/// </summary>
		/// <value>
		/// The total number of octets waiting in the tx queue.
		/// </value>
		Int64 TXOctets { get; }

		/// <summary>
		/// Gets or sets the total number of octets transmitted. Overflow of this
		/// counter will be harmless by cycles the number around.
		/// </summary>
		/// <value>
		/// The rx total number of octets.
		/// </value>
		Int64 TXTotal { get; set; }

		/// <summary>
		/// Gets or sets the number of tx errors occurred.
		/// </summary>
		/// <value>
		/// The number of tx errors occurred.
		/// </value>
		Int64 TXErrors { get; set; }

		/// <summary>
		/// Forward a frame to the channel.
		/// </summary>
		/// <returns>
		/// Frame number that temporarily identifies the frame on this interface. Wrapping may
		/// occur.
		/// </returns>
		/// <param name='frame'>
		/// Frame data.
		/// </param>
        UInt64 ForwardFrame(L2Frame frame);

		/// <summary>
		/// Try to cancel a frame that enqueued earlier with ForwardFrame.
		/// </summary>
		/// <returns>
		/// <c>true</c> when forwarding of the frame with number 'frameNo' could be
		/// cancelled; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='frameNo'>
		/// The number of the frame that shall be cancelled.
		/// </param>
		bool CancelFrame(UInt64 frameNo);

	}
}

