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
		/// Trnamitter queue.
		/// </summary>
		protected IList<L2Frame> m_txQueue = new List<L2Frame>();
		
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
		protected volatile bool m_txThreadStop;
		
		/// <summary>
		/// The runtime objcect.
		/// </summary>
		protected L2Runtime m_runtime = L2Runtime.Instance;

        /// <summary>
        /// Default target channel.
        /// </summary>
        protected IL2Channel m_target = null;
		
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="properties">Properties of the channel.
		/// <list type="bullet">
		/// <listheader>
		/// <term>Property name</term>
		/// <description>Description</description>
		/// </listheader>
		/// <item><term>Name</term><description>Name of the channel [Mandatory]</description>
		/// <item><term>Target</term><description>Where to route packages to [Default: Router]</description></item>
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
			string _target;
			if (!properties.TryGetValue("Target", out _target))
				_target = "Router";
			m_target = m_runtime.LookupChannel(_target);
			if (m_target == null)
				throw new L2InvalidPropertyException("Target not found: " + _target);
			L2Runtime.Instance.RegisterChannel(this, m_name);
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
        /// Get or set the default target channel.
        /// </summary>
        public virtual IL2Channel Target
        {
            get
            {
                return this.m_target;
            }
            set
            {
                this.m_target = value;
            }
        }

		/// <summary>
		/// Number of frames in the receiver queue.
		/// </summary>
		public virtual Int32 RXSize {
			get {
				return 0; // No buffering of receive data.
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
        /// Forward a frame to the channel.
        /// </summary>
        /// <param name="frame">Frame data.</param>
        /// <returns>Frame number.</returns>
        public virtual UInt64 ForwardFrame(L2Frame frame)
		{
            if (frame.addr == null)
                throw new ArgumentNullException("frame.addr");
            if (frame.data == null)
                throw new ArgumentNullException("frame.data");
			lock (this.m_txQueue) {
				int i = this.m_txQueue.Count;
				if (frame.isPriorityFrame)
					for (i = 0; i < this.m_txQueue.Count; ++i)
						if (!this.m_txQueue[i].isPriorityFrame)
							break;
				/*
				if (m_runtime.LogLevel >= L2LogLevel.DEBUG) {
					string text = String.Format(
						"SendFrame({0}) NO={1} AT={2}",
						L2HexConverter.ToHexString(frame.data), frame.no, i);
					m_runtime.Log(L2LogLevel.DEBUG, m_name, text);
				}
				*/
				this.m_txQueue.Insert(i, frame);
				int len = frame.data.Length;
				unchecked {
					this.m_txOctets += len;
					this.m_txTotal += len;
				}
			}
			lock (this.m_txSync) {
				Monitor.Pulse(this.m_txSync);
			}
			return frame.no;
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
					m_runtime.Log(L2LogLevel.DEBUG, m_name,
					              String.Format("Successfully cancelled frame no {0}", frameNo));
					return true;
				} else {
					m_runtime.Log(L2LogLevel.DEBUG, m_name,
					              String.Format("Unable to cancel frame no {0}", frameNo));
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
			m_runtime.Log(L2LogLevel.INFO, m_name, "Channel reset");
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
			m_runtime.Log(L2LogLevel.INFO, m_name, "Channel opened");
			if (this.m_txThread != null)
				throw new InvalidOperationException("Channel is not closed");
			this.m_txThreadStop = false;
			this.m_txThread = new Thread(new ThreadStart(this.ForwardHandler));
			this.m_txThread.Start();
		}
		
		/// <summary>
		/// Close the channel. No data will be transmitted or received. All queued
		/// data is preserved.
		/// </summary>
		public virtual void Close() {
			m_runtime.Log(L2LogLevel.INFO, m_name, "Channel close");
			if (this.m_txThread == null)
				return;
			this.m_txThreadStop = true;
			lock(this.m_txSync) {
				Monitor.PulseAll(this.m_txSync);
			}
			this.m_txThread.Interrupt();
			this.m_txThread.Join();
			this.m_txThread.Abort();
			this.m_txThread = null;
		}
		
		/// <summary>
		/// This handler is executed in the transmit thread.
		/// </summary>
		protected void ForwardHandler()
		{
			m_runtime.Log(L2LogLevel.DEBUG, m_name, "ForwardThread start");
			while (!this.m_txThreadStop) {
				L2Frame frame = L2Frame.Empty;
				lock(this.m_txQueue) {
					if (this.m_txQueue.Count > 0) {
						frame = this.m_txQueue[0];
						this.m_txQueue.RemoveAt(0);
						unchecked {
							this.m_txOctets -= frame.data.Length;
						}
					}
				} // end lock //
				if (frame.IsEmpty()) {
					lock(this.m_txSync) {
						Monitor.Wait(this.m_txSync);
					}
				} else {
					try {
						this.OnForward(frame);
					} catch (Exception ex) {
						this.OnForwardError(
							"Unable to forward frame on interface " + this.m_name, ex);
					}
				}
			} // end while //
			m_runtime.Log(L2LogLevel.INFO, m_name, "ForwardThread end");
		}

		/// <summary>
		/// Process incoming data.
		/// </summary>
		/// <param name="frame"></param>
		protected virtual void OnReceive(L2Frame frame)
		{
            if (m_target != null)
            {
            	if (m_runtime.LogLevel >= L2LogLevel.DEBUG) {
					string text = String.Format(
						"OnReceive({0}) NO={1} -> {2}",
						L2HexConverter.ToHexString(frame.data, true), frame.no, m_target.Name);
					m_runtime.Log(L2LogLevel.DEBUG, m_name, text);
            	}
                lock (m_target)
                {
                    try
                    {
                        m_target.ForwardFrame(frame);
                        unchecked
                        {
                            this.m_rxOctets += frame.data.Length;
                        }
                    }
                    catch { }
                }
            }
		}
		
		/// <summary>
		/// Actually write data to the output media.
		/// </summary>
		/// <param name="frame">Data to send</param>
		protected virtual void OnForward(L2Frame frame)
		{
			if (frame.addr == null)
				throw new ArgumentNullException("frame.addr");
			if (frame.data == null)
				throw new ArgumentNullException("frame.data");
        	if (m_runtime.LogLevel >= L2LogLevel.DEBUG) {
				string text = String.Format(
					"OnForward({0}) NO={1}",
					L2HexConverter.ToHexString(frame.data, true), frame.no);
				m_runtime.Log(L2LogLevel.DEBUG, m_name, text);
        	}
			unchecked {
				this.m_txTotal += frame.data.Length;
			}
		}
		
		/// <summary>
		/// Called in the case of a transmit error.
		/// </summary>
		/// <param name="message">Message describing the error.</param>
		/// <param name="ex">Optional error exception.</param>
		protected virtual void OnForwardError(string message, Exception ex = null)
		{
			if (ex != null)
				message = String.Format("Forward error: {0}: {1}", message, ex.Message);
			else
				message = String.Format("Forward error: {0}", message);
			m_runtime.Log(L2LogLevel.ERROR, "L2Channel", message);
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
			m_runtime.Log(L2LogLevel.ERROR, "L2Channel", message);
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
			m_runtime.Log(L2LogLevel.ERROR, "L2Channel",
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
		/// Local dispose.
		/// </summary>
		/// <param name="intern">Set to <c>true</c> when calling from user code.</param>
		protected virtual void Dispose(bool intern)
		{
		}

	}
}
