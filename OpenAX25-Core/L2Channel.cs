//
// L2Channel.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using OpenAX25Contracts;

namespace OpenAX25Core
{
	/// <summary>
	/// Standard Implementation of a L2Channel.
	/// </summary>
	public abstract class L2Channel : IL2Channel, IDisposable
	{
		/// <summary>
		/// Properties of this channel.
		/// </summary>
		protected readonly IDictionary<string, string> m_properties;
		
		/// <summary>
		/// Name of this channel.
		/// </summary>
		protected readonly string m_name;
		
		/// <summary>
		/// Number of octets in the RX queue
		/// </summary>
		protected Int64 m_rxOctets;
		
		/// <summary>
		/// Totally number of octets received.
		/// </summary>
		protected Int64 m_rxTotal;
		
		/// <summary>
		/// Totally number of receive errors.
		/// </summary>
		protected Int64 m_rxErrors;
		
		/// <summary>
		/// Number of octets in the TX queue.
		/// </summary>
		protected Int64 m_txOctets;
		
		/// <summary>
		/// Totally number of octets received.
		/// </summary>
		protected Int64 m_txTotal;
		
		/// <summary>
		/// Totally number of transmit errors.
		/// </summary>
		protected Int64 m_txErrors;
		
		/// <summary>
		/// Receiver queue.
		/// </summary>
		protected BlockingCollection<byte[]> m_rxQueue = new BlockingCollection<byte[]>();
		
		/// <summary>
		/// Trnamitter queue.
		/// </summary>
		protected IList<L2TxFrame> m_txQueue = new List<L2TxFrame>();
		
		/// <summary>
		/// Next frame number to assign.
		/// </summary>
		protected UInt64 m_frameNo;
		
		/// <summary>
		/// Lock object for TX thread synchronization.
		/// </summary>
		protected object m_txSync = new object();
		
		/// <summary>
		/// Transmitter thread.
		/// </summary>
		protected Thread m_txThread;
		
		/// <summary>
		/// Set to <c>true</c> will cancel the thread.
		/// </summary>
		protected bool m_txThreadStop;
		
		/// <summary>
		/// The runtime objcect.
		/// </summary>
		protected L2Runtime runtime = L2Runtime.Instance;
		
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="properties">Properties of the channel.
		/// <list type="bullet">
		/// <listheader>
		/// <term>Property name</term>
		/// <description>Description</description>
		/// </listheader>
		/// <item>
		/// <term>Name</term>
		/// <description>Name of the interface [mandatory]</description>
		/// </item>
		/// </list>
		/// </param>
		protected L2Channel(IDictionary<string, string> properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");
			this.m_properties = properties;
			if (!properties.TryGetValue("Name", out this.m_name))
				throw new L2MissingPropertyException("Name");
			if (String.IsNullOrEmpty(this.m_name))
				throw new L2InvalidPropertyException("Name");
		}
		
		/// <summary>
		/// Name of this interface.
		/// </summary>
		public virtual string Name {
			get {
				return this.m_name;
			}
		}
		
		/// <summary>
		/// Properties of this interface.
		/// </summary>
		public virtual IDictionary<string,string> Properties {
			get {
				return this.m_properties;
			}
		}

				/// <summary>
		/// Number of frames in the receiver queue.
		/// </summary>
		public virtual Int32 RXSize {
			get {
				return this.m_rxQueue.Count;
			}
		}

		/// <summary>
		/// Number of octets in the receiver queue.
		/// </summary>
		public virtual Int64 RXOctets {
			get {
				return this.m_rxOctets;
			}
		}

		/// <summary>
		/// Total number of octets received on this interface.
		/// </summary>
		public virtual Int64 RXTotal {
			get {
				return this.m_rxTotal;
			}
			set {
				this.m_rxTotal = value;
			}
		}

		/// <summary>
		/// Total number of receive errors on this interface.
		/// </summary>
		public virtual Int64 RXErrors {
			get {
				return this.m_rxErrors;
			}
			set {
				this.m_rxErrors = value;
			}
		}

		/// <summary>
		/// Number of frames in the transmitter queue.
		/// </summary>
		public virtual Int32 TXSize {
			get {
				return this.m_txQueue.Count;
			}
		}

		/// <summary>
		/// Number of octets in the transmitter queue.
		/// </summary>
		public virtual Int64 TXOctets {
			get {
				return this.m_txOctets;
			}
		}

		/// <summary>
		/// Total number of octets transmitted on this interface.
		/// </summary>
		public virtual Int64 TXTotal {
			get {
				return this.m_txTotal;
			}
			set {
				this.m_txTotal = value;
			}
		}

		/// <summary>
		/// Total number of transmit errors on this interface.
		/// </summary>
		public virtual Int64 TXErrors {
			get {
				return this.m_txErrors;
			}
			set {
				this.m_txErrors = value;
			}
		}

		/// <summary>
		/// Receive a frame on this interface.
		/// </summary>
		/// <param name="blocking">If set to <c>true</c> the call blocks until
		/// there is a frame available on the receiver queue; Otherwise returns
		/// <c>null</c></param>
		/// <returns>Next received frame or null if nothing is pending and
		/// <c>blocking</c> is <c>false</c>.</returns>
		public virtual byte[] ReceiveFrame(bool blocking)
		{
			byte[] frame;
			if (blocking)
				frame = this.m_rxQueue.Take();
			else if (!this.m_rxQueue.TryTake(out frame))
				frame = null;
			if (frame != null)
				unchecked {
					this.m_rxOctets -= frame.Length;
				}
			return frame;
		}

		/// <summary>
		/// Send a frame over the channel.
		/// </summary>
		/// <param name="frame">The frame to send</param>
		/// <param name="blocking">If <c>true</c> the call is blocking until a frame
		/// is available on the channel; otherwise returning <c>null</c> in this case.</param>
		/// <param name="priority">If <c>true</c> then send this frame prior to any non prior
		/// frames on the channel. A transmission in progress is not interrupted. </param>
		/// <returns>Frame number that identifies the frame for a later reference.
		/// The number is rather long, however wrap around is not impossible.</returns>
		public virtual UInt64 SendFrame (byte[] frame, bool blocking, bool priority)
		{
			if (frame == null)
				throw new ArgumentNullException("frame");
			UInt64 id = this.NewFrameNo();
			L2TxFrame tf = new L2TxFrame(id, priority, frame);
			lock (this.m_txQueue) {
				int i = this.m_txQueue.Count;
				if (priority)
					for (i = 0; i < this.m_txQueue.Count; ++i)
						if (!this.m_txQueue[i].isPriorityFrame)
							break;
				this.m_txQueue.Insert(i, tf);
				int l = frame.Length;
				unchecked {
					this.m_txOctets += l;
					this.m_txTotal += 1;
				}
			}
			lock (this.m_txSync) {
				Monitor.Pulse (this.m_txSync);
			}
			return id;
		}

		/// <summary>
		/// Try to cancel the transmittion of a frame. If the frame is in the
		/// progress of transmission or transmitted already, <c>false</c> is
		/// returned.
		/// </summary>
		/// <param name="frameNo">The frame number of the frame to cancel.</param>
		/// <returns><c>true</c> if the frame could be cancelled; <c>false</c>
		/// otherwise</returns>
		public virtual bool CancelFrame(UInt64 frameNo)
		{
			lock (this.m_txQueue) {
				int i = -1;
				for (int j = 0; j < this.m_txQueue.Count; ++j)
					if (this.m_txQueue[j].no == frameNo) {
						i = j;
						break;
					}
				if (i >= 0) {
					int l = this.m_txQueue[i].data.Length;
					this.m_txQueue.RemoveAt(i);
					unchecked {
						this.m_txOctets -= l;
						this.m_txTotal -= 1;
					}
					return true;
				} else {
					return false;
				}
			}
		}


		/// <summary>
		/// Reset the channel and withdraw all frames in the
		/// receiver and transmitter queue.
		/// </summary>
		public virtual void Reset()
		{
			lock (this) {
				this.Close();
				byte[] dummy;
				while (this.m_rxQueue.TryTake(out dummy)) {}
				this.m_rxOctets = 0;
				this.m_txQueue.Clear();
				this.m_txOctets = 0;
				this.Open();
			}
		}

		/// <summary>
		/// Reset the channel and withdraw all frames in the
		/// receiver queue. All frames in the transmitter queue
		/// are preserved and will be sent on the new connection.
		/// </summary>
		public virtual void ResetRX()
		{
			lock (this) {
				this.Close();
				byte[] dummy;
				while (this.m_rxQueue.TryTake(out dummy)) {}
				this.m_rxOctets = 0;
				this.Open();
			}
		}

		/// <summary>
		/// Reset the channel and withdraw all frames in the
		/// transmitter queue. All frames in the receiver queue
		/// are preserved and will be sent on the new connection.
		/// </summary>
		public virtual void ResetTX()
		{
			lock (this) {
				this.Close();
				this.m_txQueue.Clear();
				this.m_txOctets = 0;
				this.Open();
			}
		}

		/// <summary>
		/// Open the channel, so that data actually be transmitted and received.
		/// </summary>
		public virtual void Open() {
			if (this.m_txThread != null)
				throw new InvalidOperationException("Channel is not closed");
			this.m_txThreadStop = false;
			this.m_txThread = new Thread(new ThreadStart(this.TransmitHandler));
			this.m_txThread.Start();
		}
		
		/// <summary>
		/// Close the channel. No data will be transmitted or received. All queued
		/// data is preserved.
		/// </summary>
		public virtual void Close() {
			if (this.m_txThread == null)
				return;
			this.m_txThreadStop = true;
			lock(this.m_txSync) {
				Monitor.Pulse(this.m_txSync);
			}
			this.m_txThread.Join();
			this.m_txThread = null;
		}
		
		/// <summary>
		/// This handler is executed in the transmit thread.
		/// </summary>
		protected void TransmitHandler()
		{
			while (!this.m_txThreadStop) {
				byte[] frame = null;
				lock(this.m_txQueue) {
					if (this.m_txQueue.Count > 0) {
						frame = this.m_txQueue[0].data;
						this.m_txQueue.RemoveAt(0);
						unchecked {
							this.m_txOctets -= frame.Length;
						}
					}
				} // end lock //
				if (frame == null) {
					lock(this.m_txSync) {
						Monitor.Wait(this.m_txSync);
					}
				} else {
					try {
						this.OnTransmit(frame);
					} catch (Exception ex) {
						this.OnTransmitError(
							"Unable to transmit frame on interface " + this.m_name, ex);
					}
				}
			} // end while //
		}

		/// <summary>
		/// Put received data frame into the receive queue.
		/// </summary>
		/// <param name="frame"></param>
		protected virtual void OnReceive(byte[] frame)
		{
			lock(this) {
				this.m_rxQueue.Add(frame);
				unchecked {
					this.m_rxOctets += frame.Length;
				}
			}
		}
		
		/// <summary>
		/// Actually write data to the output media.
		/// </summary>
		/// <param name="data">Data to send</param>
		protected virtual void OnTransmit(byte[] data)
		{
				unchecked {
					this.m_txTotal += data.Length;
				}
		}
		
		/// <summary>
		/// Called in the case of a transmit error.
		/// </summary>
		/// <param name="message">Message describing the error.</param>
		/// <param name="ex">Optional error exception.</param>
		protected virtual void OnTransmitError(string message, Exception ex = null)
		{
			if (ex != null)
				message = String.Format("TX error: {0}: {1}", message, ex.Message);
			else
				message = String.Format("TX error: {0}", message);
			runtime.Log(L2LogLevel.ERROR, "L2Channel", message);
			unchecked {
				this.m_txErrors += 1;
			}
		}

		/// <summary>
		/// Called in the case of a receive error.
		/// </summary>
		/// <param name="message">Message describing the error.</param>
		/// <param name="ex">Optional error exception.</param>
		protected virtual void OnReceiveError(string message, Exception ex = null)
		{
			if (ex != null)
				message = String.Format("RX error: {0}: {1}", message, ex.Message);
			else
				message = String.Format("RX error: {0}", message);
			runtime.Log(L2LogLevel.ERROR, "L2Channel", message);
			unchecked {
				this.m_rxErrors += 1;
			}
		}
		
		/// <summary>
		/// Called in the case of a receive error.
		/// </summary>
		/// <param name="message">Message describing the error.</param>
		/// <param name="data">data to dump.</param>
		protected virtual void OnReceiveError(string message, byte[] data)
		{
			runtime.Log(L2LogLevel.ERROR, "L2Channel",
			            String.Format("RX error: {0}: {1}", message, L2HexConverter.ToHexString(data)));
			unchecked {
				this.m_rxErrors += 1;
			}
		}

		/// <summary>
		/// Dispose this object, when it is not longer needed.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		/// <summary>
		/// Get a new frame no.
		/// </summary>
		/// <returns>New frame number.</returns>
		protected virtual UInt64 NewFrameNo()
		{
			lock (this) {
				unchecked {
					return this.m_frameNo++;
				}
			}
		}

		/// <summary>
		/// Local dispose.
		/// </summary>
		/// <param name="intern">Set to <c>true</c> when calling from user code.</param>
		protected virtual void Dispose(bool intern)
		{
			if (intern) {
				m_rxQueue.Dispose();
			}
		}

	}
}
