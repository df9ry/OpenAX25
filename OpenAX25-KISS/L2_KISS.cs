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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.IO;
using System.Threading;

using OpenAX25Contracts;

namespace OpenAX25KISS
{
	public class L2_KISS : IL2Channel
	{
		private const int RAW_BUFFER_CHUNK = 256;
		private const int FRAME_BUFFER_CHUNK = 1024;
		private const byte FEND = 0xc0;
		private const byte FESC = 0xdb;
		private const byte TFEND = 0xdc;
		private const byte TFESC = 0xdd;
		private string name;
		private string ifcName;
		private int baudRate;
		private SerialPort port = null;
		private Int64 rxOctets = 0;
		private Int64 rxTotal = 0;
		private Int64 rxErrors = 0;
		private Int64 txOctets = 0;
		private Int64 txTotal = 0;
		private Int64 txErrors = 0;
		private UInt64 frameNo = 0;
		private byte[] rxRawBuffer = new byte[RAW_BUFFER_CHUNK];
		private byte[] rxFrameBuffer = new byte[FRAME_BUFFER_CHUNK];
		private byte[] txFrameBuffer = new byte[FRAME_BUFFER_CHUNK];
		private int iFrameBuffer = 0;
		private bool fEscape = false;
		private bool fInFrame = false;
		private IDictionary<string,object>properties = new Dictionary<string, object>();
		private BlockingCollection<byte[]> rxQueue = new BlockingCollection<byte[]>();
		private IList<TxFrame> txQueue = new List<TxFrame>();
		private Thread txThread = null;
		private object txSync = new object();

		public L2_KISS (string name, string ifcName, int baudRate)
		{
			this.name = name;
			this.ifcName = ifcName;
			this.baudRate = baudRate;
			OpenChannel();
		}

		public string Name {
			get {
				return this.name;
			}
		}

		public Int32 RxSize {
			get {
				return this.rxQueue.Count;
			}
		}

		public Int64 RxOctets {
			get {
				return this.rxOctets;
			}
		}

		public Int64 RxTotal {
			get {
				return this.rxTotal;
			}
			set {
				this.rxTotal = value;
			}
		}

		public Int64 RxErrors {
			get {
				return this.rxErrors;
			}
			set {
				this.rxErrors = value;
			}
		}

		public Int32 TxSize {
			get {
				return this.txQueue.Count;
			}
		}

		public Int64 TxOctets {
			get {
				return this.txOctets;
			}
		}

		public Int64 TxTotal {
			get {
				return this.txTotal;
			}
			set {
				this.txTotal = value;
			}
		}

		public Int64 TxErrors {
			get {
				return this.txErrors;
			}
			set {
				this.txErrors = value;
			}
		}

		public IDictionary<string,object> Properties {
			get {
				return this.properties;
			}
		}

		public byte[] ReceiveFrame (bool blocking)
		{
			byte[] frame;
			if (blocking)
				frame = this.rxQueue.Take();
			else if (!this.rxQueue.TryTake(out frame))
				frame = null;
			if (frame != null)
				unchecked {
					this.rxOctets -= frame.Length;
				}
			return frame;
		}

		public UInt64 SendFrame (byte[] frame, bool blocking, bool priority)
		{
			if (frame == null)
				throw new ArgumentNullException ("frame");
			UInt64 id = this.NewFrameNo();
			TxFrame tf = new TxFrame(id, priority, Encode(frame));
			lock (this.txQueue) {
				int i = this.txQueue.Count;
				if (priority)
					for (i = 0; i < this.txQueue.Count; ++i)
						if (!this.txQueue[i].isPriorityFrame)
							break;
				this.txQueue.Insert(i, tf);
				int l = frame.Length;
				unchecked {
					this.txOctets += l;
					this.txTotal += 1;
				}
			}
			lock (this.txSync) {
				Monitor.Pulse (this.txSync);
			}
			return id;
		}

		public bool CancelFrame (UInt64 frameNo)
		{
			lock (this.txQueue) {
				int i = -1;
				for (int j = 0; j < this.txQueue.Count; ++j)
					if (this.txQueue[j].no == frameNo) {
						i = j;
						break;
					}
				if (i >= 0) {
					int l = this.txQueue[i].data.Length;
					this.txQueue.RemoveAt(i);
					unchecked {
						this.txOctets -= l;
						this.txTotal -= 1;
					}
					return true;
				} else {
					return false;
				}
			}
		}

		public void ResetChannel ()
		{
			lock (this) {
				CloseChannel ();
				byte[] dummy;
				while (this.rxQueue.TryTake(out dummy)) {
				}
				//FlushTxBuffers();
				OpenChannel ();
			}
		}

		public void ResetRxChannel ()
		{
			lock (this) {
				CloseChannel ();
				byte[] dummy;
				while (this.rxQueue.TryTake(out dummy)) {
				}
				OpenChannel ();
			}
		}

		public void ResetTxChannel ()
		{
			lock (this) {
				CloseChannel ();
				//FlushTxBuffers();
				OpenChannel ();
			}
		}

		private void TransmitHandler ()
		{
			while (true) {
				byte[] frame = null;
				lock(this.txQueue) {
					if (this.txQueue.Count > 0) {
						frame = this.txQueue[0].data;
						this.txQueue.RemoveAt(0);
						unchecked {
							this.txOctets -= frame.Length;
						}
					}
				} // end lock //
				if (frame == null) {
					lock(this.txSync) {
						Monitor.Wait(this.txSync);
					}
				} else {
					try {
						this.port.Write(frame, 0, frame.Length);
						/*
						if (frame.Length > 0) {
							byte[] monitorFrame = new byte[frame.Length];
							Array.Copy (frame, monitorFrame, frame.Length);
							monitorFrame[0] = 0x10;
							this.HandleRxFrame(monitorFrame, frame.Length);
						}
						*/
					} catch (ArgumentNullException) {
						// Buffer was null:
						HandleRxFrame (new byte[] { 0xff, 0x01 }, 2);
					} catch (InvalidOperationException) {
						// The port is closed:
						HandleRxFrame (new byte[] { 0xff, 0x02 }, 2);
					} catch (ArgumentOutOfRangeException) {
						// offset or count is illogical:
						HandleRxFrame (new byte[] { 0xff, 0x03 }, 2);
					} catch (ArgumentException) {
						// offset + count > data.Length:
						HandleRxFrame (new byte[] { 0xff, 0x04 }, 2);
					} catch (TimeoutException) {
						// Send timeout:
						HandleRxFrame (new byte[] { 0xff, 0x05 }, 2);
					} catch {
						// Unknown exception:
						HandleRxFrame (new byte[] { 0xff, 0x06 }, 2);
					}
				}
			} // end while //
		}

		private void DataReceivedHandler (object sender, SerialDataReceivedEventArgs e)
		{
			SerialPort sp = (SerialPort)sender;
			try {
				while (true) {
					int nBytes = sp.BytesToRead;
					if (nBytes == 0)
						break;
					if (nBytes > this.rxRawBuffer.Length)
						this.rxRawBuffer = new byte[( nBytes / RAW_BUFFER_CHUNK + 1 ) * 
						                         RAW_BUFFER_CHUNK];
					int bytesRead = sp.Read (this.rxRawBuffer, 0, nBytes);
					for (int i = 0; i < bytesRead; ++i)
						HandleRxByte(this.rxRawBuffer[i]);
				} // end while //
			} catch (IOException) {
				// Invalid com state:
				HandleRxFrame (new byte[] { 0xff, 0x11 }, 2);
			} catch (InvalidOperationException) {
				// Com is closed:
				HandleRxFrame (new byte[] { 0xff, 0x12 }, 2);
			} catch {
				// Unknown driver error
				HandleRxFrame (new byte[] { 0xff, 0x13 }, 2);
			}
		}

		private void ErrorReceivedHandler (object sender, SerialErrorReceivedEventArgs e)
		{
			SerialPort sp = (SerialPort)sender;
			switch (e.EventType) {
			case SerialError.TXFull:
				// Handle TX Full event
				HandleRxFrame (new byte[] { 0xff, 0xf1 }, 2);
				break;
			case SerialError.RXOver:
				// RX buffer full. Cancel current frame and re-sync:
				HandleRxFrame (new byte[] { 0xff, 0xf2 }, 2);
				this.iFrameBuffer = 0;
				this.fInFrame = false;
				unchecked {
					this.rxErrors += 1;
				}
				break;
			case SerialError.Overrun:
				// RX overrun. Cancel current frame and re-sync:
				HandleRxFrame (new byte[] { 0xff, 0xf3 }, 2);
				this.iFrameBuffer = 0;
				this.fInFrame = false;
				unchecked {
					this.rxErrors += 1;
				}
				break;
			case SerialError.RXParity:
				// RX parity error. Cancel current frame and re-sync:
				HandleRxFrame (new byte[] { 0xff, 0xf4 }, 2);
				this.iFrameBuffer = 0;
				this.fInFrame = false;
				unchecked {
					this.rxErrors += 1;
				}
				break;
			case SerialError.Frame:
				// RX framing error. Cancel current frame and re-sync:
				HandleRxFrame (new byte[] { 0xff, 0xf5 }, 2);
				this.iFrameBuffer = 0;
				this.fInFrame = false;
				unchecked {
					this.rxErrors += 1;
				}
				break;
			} // end switch //
		}

		private void HandleRxByte (byte b)
		{
			if (this.fInFrame) {
				if (this.fEscape) {
					switch (b) {
					case TFEND :
						AppendRxByte(FEND);
						break;
					case TFESC :
						AppendRxByte(FESC);
						break;
					default : // Protocol error:
						unchecked {
							this.RxErrors += 1;
						}
						this.fInFrame = false;
						break;
					} // end switch //
				} else { // Not escape //
					switch (b) {
					case FEND :
						if (this.iFrameBuffer > 0) {
							HandleRxFrame(this.rxFrameBuffer, this.iFrameBuffer);
							this.fInFrame = false;
						}
						break;
					case FESC :
						this.fEscape = true;
						break;
					default :
						AppendRxByte(b);
						break;
					} // end switch //
				}
			} else { // Not in frame //
				if (b == FEND) {
					this.fInFrame = true;
					this.iFrameBuffer = 0;
				}
			}
		}

		private void AppendRxByte (byte b)
		{
			if (this.iFrameBuffer >= this.rxFrameBuffer.Length) {
				byte[] newRxFrameBuffer = new byte[((this.iFrameBuffer + 1) / FRAME_BUFFER_CHUNK + 1) *
					FRAME_BUFFER_CHUNK];
				Array.Copy (this.rxFrameBuffer, newRxFrameBuffer, this.iFrameBuffer);
				this.rxFrameBuffer = newRxFrameBuffer;
			}
			this.rxFrameBuffer [this.iFrameBuffer++] = b;
			unchecked {
				this.rxTotal += 1;
			}
		}

		private void HandleRxFrame (byte[] rxFrameBuffer, int iFrameBuffer)
		{
			lock(this) {
				byte[] frame = new byte[iFrameBuffer];
				Array.Copy(rxFrameBuffer, frame, iFrameBuffer);
				this.rxQueue.Add(frame);
				unchecked {
					this.rxOctets += iFrameBuffer;
				}
			}
		}

		private void CloseChannel ()
		{
			lock(this) {
				if (this.txThread != null) {
					this.txThread.Abort ();
					this.txThread = null;
				}
				if (this.port != null) {
					this.port.Close ();
					this.port.Dispose ();
					this.port = null;
				}
			}
		}

		private void OpenChannel ()
		{
			lock(this) {
				this.fInFrame = false;
				this.fEscape = false;
				this.iFrameBuffer = 0;
				this.port = new SerialPort(ifcName, this.baudRate);
				this.port.Parity = Parity.None;
				this.port.StopBits = StopBits.One;
				this.port.DataBits = 8;
				this.port.Handshake = Handshake.None;
				this.port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
				this.port.ReadBufferSize = 256;
				this.port.ReadTimeout = 200;
				this.port.ReceivedBytesThreshold = 1;
				this.port.WriteTimeout = 10000;
				this.port.WriteBufferSize = 256;
				this.port.DiscardNull = false;
				this.port.Open();
				this.txThread = new Thread(new ThreadStart(this.TransmitHandler));
				this.txThread.Start();
			}
		}

		private UInt64 NewFrameNo ()
		{
			lock (this) {
				unchecked {
					return this.frameNo++;
				}
			}
		}

		private byte[] Encode (byte[] frame)
		{
			byte[] result;
			lock (this.txFrameBuffer) {
				int j = EncodeAdd(0, FEND);
				foreach (byte b in frame) {
					switch (b) {
					case FEND :
						j = EncodeAdd(j, FESC);
						j = EncodeAdd(j, TFEND);
						break;
					case FESC :
						j = EncodeAdd(j, FESC);
						j = EncodeAdd(j, TFESC);
						break;
					default :
						j = EncodeAdd(j, b);
						break;
					} // end switch //
				} // end foreach //
				j = EncodeAdd(j, FEND);
				result = new byte[j];
				Array.Copy (this.txFrameBuffer, result, j);
			}
			return result;
		}

		private int EncodeAdd (int j, byte b)
		{
			if (j >= this.txFrameBuffer.Length) {
				byte[] newFrameBuffer =
					new byte[((j + 1) / FRAME_BUFFER_CHUNK + 1) * FRAME_BUFFER_CHUNK];
				Array.Copy (this.txFrameBuffer, newFrameBuffer, j);
				this.txFrameBuffer = newFrameBuffer;
			}
			this.txFrameBuffer[j++] = b;
			return j;
		}


	}
}

