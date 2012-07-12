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
	/// I l2 channel.
	/// Data channel to transmit and receive transparent frames of octets. The data
	/// in the frames is checked against corruptions on the link layer only. No
	/// interpretion of the channel content is performed.
	/// </summary>
	public interface IL2Channel
	{
		/// <summary>
		/// Gets the name of the channel. This name have to be unique accross the
		/// application and can never change. There is no interpretion or syntax check
		/// performed.
		/// </summary>
		/// <value>
		/// The unique name of this channel.
		/// </value>
		string Name { get; }

		/// <summary>
		/// Gets the properties of this channel.
		/// </summary>
		/// <value>
		/// The properties.
		/// </value>
		IDictionary<string,string> Properties { get; }
		
		/// <summary>
		/// Open the interface.
		/// </summary>
		void Open();
		
		/// <summary>
		/// Close the interface.
		/// </summary>
		void Close();

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
		/// Get frame from the receiver queue.
		/// </summary>
		/// <returns>
		/// The next frame, if one is available: otherwise <c>null</c>.
		/// </returns>
		/// <param name='blocking'>
		/// When there is no data on the receive queue and 'blocking' is <c>true</c>
		/// the call blocks until there is data available.
		/// </param>
		byte[] ReceiveFrame(bool blocking);

		/// <summary>
		/// Send a frame over the channel.
		/// </summary>
		/// <returns>
		/// Frame number that temporarily identifies the frame on this interface. Wrapping may
		/// occur.
		/// </returns>
		/// <param name='frame'>
		/// Frame data.
		/// </param>
		/// <param name='blocking'>
		/// If there is no room left on the transmit queue 'blocking' controls the behavior.
		/// When set to <c>true'</c> the call blocks until space becomes available. Otherwise an
		/// 'L2NoSpaceException' is thrown.
		/// </param>
		/// <param name='priority'>
		/// Priority.
		/// </param>
		UInt64 SendFrame(byte[] frame, bool blocking, bool priority);

		/// <summary>
		/// Try to cancel a frame that enqueued earlier with SendFrame.
		/// </summary>
		/// <returns>
		/// <c>true</c> when transmission of the frame with number 'frameNo' could be
		/// cancelled; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='frameNo'>
		/// The number of the frame that shall be cancelled.
		/// </param>
		bool CancelFrame(UInt64 frameNo);

		/// <summary>
		/// Resets the channel. The data link is closed and reopened. All pending
		/// data is withdrawn.
		/// </summary>
		void Reset();

		/// <summary>
		/// Resets the channel and clears the receiver buffer. Pending transmit data
		/// is preserved.
		/// </summary>
		void ResetRX();

		/// <summary>
		/// Resets the channel and clears the transmitter buffer. Pending received data
		/// is presserved.
		/// </summary>
		void ResetTX();
	}
}

