//
// KissChannel.cs
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
using System.Collections.Concurrent;

using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25Kiss
{
	/// <summary>
	/// Description of KissChannel.
	/// </summary>
	public class KissChannel : IL2Channel
	{

		private static readonly byte[] CHANNEL_ID = {
			0x00,0x10,0x20,0x30,0x40,0x50,0x60,0x70,0x80,0x90,0xa0,0xb0,0xc0,0xd0,0xe0,0xf0
		};
		
		private byte m_channelId;
		private KissInterface m_ifc;
		private int m_channelNo;
		private string m_name;
		private BlockingCollection<byte[]> m_rxQueue = new BlockingCollection<byte[]>();
		
		private Int64 m_rxTotal;
		private Int64 m_txTotal;
		private Int64 m_rxOctets;

		internal KissChannel(KissInterface ifc, int channelNo)
		{
			if ((channelNo < 0) || (channelNo > 15))
				throw new ArgumentOutOfRangeException("channelNo");
			m_ifc = ifc;
			m_channelNo = channelNo;
			m_channelId = CHANNEL_ID[channelNo];
			m_name = String.Format("{0}[{1}]", ifc.Name, L2HexConverter.HEX_uc[channelNo]);
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
		public IDictionary<string,string> Properties { get { return m_ifc.Properties; } }
		
		/// <summary>
		/// Open the interface.
		/// <remarks>Only allowed on port 0 of the KISS interface</remarks>
		/// </summary>
		public void Open()
		{
			if (m_channelNo != 0)
				throw new InvalidOperationException("KISS administration is only allowed on port 0");
			m_ifc.Open();
		}
		
		/// <summary>
		/// Close the interface.
		/// <remarks>Only allowed on port 0 of the KISS interface</remarks>
		/// </summary>
		public void Close()
		{
			if (m_channelNo != 0)
				throw new InvalidOperationException("KISS administration is only allowed on port 0");
			m_ifc.Close();
		}

		/// <summary>
		/// Gets the number of frames available in the rx queue.
		/// </summary>
		/// <value>
		/// The number of frames available in the rx queue.
		/// </value>
		public Int32 RXSize {
			get {
				m_ifc.CopyOver();
				return m_rxQueue.Count;
			}
		}

		/// <summary>
		/// Gets the total number of octets available in the rx queue.
		/// </summary>
		/// <value>
		/// The total number of octets available in the rx queue.
		/// </value>
		public Int64 RXOctets {
			get {
				m_ifc.CopyOver();
				return m_rxOctets;
			}
		}

		/// <summary>
		/// Gets or sets the total number of octets received. Overflow of this
		/// counter will be harmless by cycles the number around.
		/// </summary>
		/// <value>
		/// The rx total number of octets.
		/// </value>
		public Int64 RXTotal {
			get {
				m_ifc.CopyOver();
				return m_rxTotal;
			}
			set {
				m_rxTotal = value;
			}
		}

		/// <summary>
		/// Gets or sets the number of rx errors occurred.
		/// <remarks>The number counts for all channels on this interface, for it is not
		/// possible to distinguish the channel in this place.</remarks>
		/// <remarks>Setting this value is allowed on port 0 of the KISS interface</remarks>
		/// </summary>
		/// <value>
		/// The number of rx errors occurred.
		/// </value>
		public Int64 RXErrors {
			get {
				return m_ifc.RXErrors;
			}
			set {
				if (m_channelNo != 0)
					throw new InvalidOperationException("KISS administration is only allowed on port 0");
				m_ifc.RXErrors = value;
			}
		}

		/// <summary>
		/// Gets the number of frames waiting in the tx queue.
		/// </summary>
		/// <value>
		/// The number of frames waiting in the tx queue.
		/// </value>
		public Int32 TXSize { get { return m_ifc.GetTXSizeForChannel(m_channelId); } }

		/// <summary>
		/// Gets the total number of octets waiting in the tx queue.
		/// </summary>
		/// <value>
		/// The total number of octets waiting in the tx queue.
		/// </value>
		public Int64 TXOctets { get { return m_ifc.GetTXOctetsForChannel(m_channelId); } }

		/// <summary>
		/// Gets or sets the total number of octets transmitted. Overflow of this
		/// counter will be harmless by cycles the number around.
		/// </summary>
		/// <value>
		/// The rx total number of octets.
		/// </value>
		public Int64 TXTotal {
			get { return m_txTotal; }
			set { m_txTotal = value; }
		}

		/// <summary>
		/// Gets or sets the number of tx errors occurred.
		/// <remarks>The number counts for all channels on this interface, for it is not
		/// possible to distinguish the channel in this place.</remarks>
		/// <remarks>Setting this value is allowed on port 0 of the KISS interface</remarks>
		/// </summary>
		/// <value>
		/// The number of tx errors occurred.
		/// </value>
		public Int64 TXErrors {
			get {
				return m_ifc.TXErrors;
			}
			set {
				if (m_channelNo != 0)
					throw new InvalidOperationException("KISS administration is only allowed on port 0");
				m_ifc.TXErrors = value;
			}
		}

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
		public byte[] ReceiveFrame(bool blocking)
		{
			m_ifc.CopyOver();
			byte[] frame;
			if (blocking)
				frame = this.m_rxQueue.Take();
			else if (!this.m_rxQueue.TryTake(out frame))
				frame = null;
			if (frame != null) {
				int l = frame.Length;
				unchecked {
					this.m_rxOctets -= l;
				}
				byte[] _frame = new Byte[l-1];
				Array.Copy(frame, 1, _frame, 0, l-1);
				frame = _frame;
			}
			return frame;
		}

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
		public UInt64 SendFrame(byte[] frame, bool blocking, bool priority)
		{
			if (frame == null)
				throw new ArgumentNullException("frame");
			byte[] kissFrame = new byte[frame.Length];
			kissFrame[0] = m_channelId;
			Array.Copy(frame, 0, kissFrame, 1, frame.Length);
			unchecked {
				m_txTotal += frame.Length;
			}
			return m_ifc.SendFrame(kissFrame, blocking, priority);
		}

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
		public bool CancelFrame(UInt64 frameNo)
		{
			return m_ifc.CancelFrame(frameNo);
		}

		/// <summary>
		/// Resets the channel. The data link is closed and reopened. All pending
		/// data is withdrawn.
		/// <remarks>Only allowed on port 0 of the KISS interface</remarks>
		/// </summary>
		public void Reset()
		{
			if (m_channelNo != 0)
				throw new InvalidOperationException("KISS administration is only allowed on port 0");
			m_ifc.Reset();
		}

		/// <summary>
		/// Resets the channel and clears the receiver buffer. Pending transmit data
		/// is preserved.
		/// <remarks>Only allowed on port 0 of the KISS interface</remarks>
		/// </summary>
		public void ResetRX()
		{
			if (m_channelNo != 0)
				throw new InvalidOperationException("KISS administration is only allowed on port 0");
			m_ifc.ResetRX();
		}

		/// <summary>
		/// Resets the channel and clears the transmitter buffer. Pending received data
		/// is presserved.
		/// <remarks>Only allowed on port 0 of the KISS interface</remarks>
		/// </summary>
		public void ResetTX()
		{
			if (m_channelNo != 0)
				throw new InvalidOperationException("KISS administration is only allowed on port 0");
			m_ifc.ResetTX();
		}
		
				/// <summary>
		/// Put received data frame into the receive queue.
		/// </summary>
		/// <param name="frame"></param>
		internal void OnReceive(byte[] frame)
		{
			lock(this) {
				this.m_rxQueue.Add(frame);
				unchecked {
					this.m_rxOctets += frame.Length;
				}
			}
		}
		

	}
}
