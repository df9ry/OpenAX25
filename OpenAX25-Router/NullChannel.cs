//
// NullChannel
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

namespace OpenAX25Router
{
	/// <summary>
	/// The dummy channel.
	/// </summary>
	internal class NullChannel : IL2Channel
	{
	
		private IDictionary<string,string> m_properties = new Dictionary<string,string>();
		
		internal NullChannel()
		{
			m_properties.Add("Name", Name);
			L2Runtime.Instance.RegisterChannel(this, Name);
		}
		
		/// <summary>
		/// Gets the name of the channel. This name have to be unique accross the
		/// application and can never change. There is no interpretion or syntax check
		/// performed.
		/// </summary>
		/// <value>
		/// The unique name of this channel.
		/// </value>
		public string Name { get { return "NULL"; } }

		/// <summary>
		/// Gets the properties of this channel.
		/// </summary>
		/// <value>
		/// The properties.
		/// </value>
		public IDictionary<string,string> Properties { get { return m_properties; } }

        /// <summary>
        /// Get or set the target channel for received data.
        /// </summary>
        public IL2Channel Target { get { return null; } set {} }
		
		/// <summary>
		/// Open the interface.
		/// </summary>
		public void Open() {}
		
		/// <summary>
		/// Close the interface.
		/// </summary>
		public void Close() {}

		/// <summary>
		/// Gets the number of frames available in the rx queue.
		/// </summary>
		/// <value>
		/// The number of frames available in the rx queue.
		/// </value>
		public Int32 RXSize { get { return 0; } }

		/// <summary>
		/// Gets the total number of octets available in the rx queue.
		/// </summary>
		/// <value>
		/// The total number of octets available in the rx queue.
		/// </value>
		public Int64 RXOctets { get { return 0; } }

		/// <summary>
		/// Gets or sets the total number of octets received. Overflow of this
		/// counter will be harmless by cycles the number around.
		/// </summary>
		/// <value>
		/// The rx total number of octets.
		/// </value>
		public Int64 RXTotal { get { return 0; } set {} }

		/// <summary>
		/// Gets or sets the number of rx errors occurred.
		/// </summary>
		/// <value>
		/// The number of rx errors occurred.
		/// </value>
		public Int64 RXErrors { get { return 0; } set {} }

		/// <summary>
		/// Gets the number of frames waiting in the tx queue.
		/// </summary>
		/// <value>
		/// The number of frames waiting in the tx queue.
		/// </value>
		public Int32 TXSize { get { return 0; } }

		/// <summary>
		/// Gets the total number of octets waiting in the tx queue.
		/// </summary>
		/// <value>
		/// The total number of octets waiting in the tx queue.
		/// </value>
		public Int64 TXOctets { get { return 0; } }

		/// <summary>
		/// Gets or sets the total number of octets transmitted. Overflow of this
		/// counter will be harmless by cycles the number around.
		/// </summary>
		/// <value>
		/// The rx total number of octets.
		/// </value>
		public Int64 TXTotal { get { return 0; } set {} }

		/// <summary>
		/// Gets or sets the number of tx errors occurred.
		/// </summary>
		/// <value>
		/// The number of tx errors occurred.
		/// </value>
		public Int64 TXErrors { get { return 0; } set {} }

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
		public UInt64 ForwardFrame(L2Frame frame)
		{
			return 0;
		}

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
		public bool CancelFrame(UInt64 frameNo)
		{
			return false;
		}

		/// <summary>
		/// Resets the channel. The data link is closed and reopened. All pending
		/// data is withdrawn.
		/// </summary>
		public void Reset()
		{
			
		}

		
	}
}
