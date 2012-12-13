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
using System.IO;
using System.IO.Ports;

using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25Kiss
{
	/// <summary>
	/// This is a KISS channel.
	/// </summary>
	public class KissChannel : L2Channel
	{
		private const int RAW_BUFFER_CHUNK = 256;
		private const int FRAME_BUFFER_CHUNK = 1024;
		private const byte FEND = 0xc0;
		private const byte FESC = 0xdb;
		private const byte TFEND = 0xdc;
		private const byte TFESC = 0xdd;
		private string comPort;
		private int baudrate;
		private int kissPort;
		private byte[] rxRawBuffer = new byte[RAW_BUFFER_CHUNK];
		private byte[] rxFrameBuffer = new byte[FRAME_BUFFER_CHUNK];
		private byte[] txFrameBuffer = new byte[FRAME_BUFFER_CHUNK];
		private int iFrameBuffer;
		private bool fEscape;
		private bool fInFrame;
		
		internal SerialPort port;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="properties">Properties of the channel.
		/// <list type="bullet">
		///   <listheader><term>Property name</term><description>Description</description></listheader>
		///   <item><term>Name</term><description>Name of the channel [Mandatory]</description></item>
		///   <item><term>Target</term><description>Where to route packages to [Default: ROUTER]</description></item>
		///   <item><term>ComPort</term><description>Name of the COM port [Default: COM1]</description></item>
		///   <item><term>Baudrate</term><description>Baud rate [Default: 9600]</description></item>
		///   <item><term>Port</term><description>Fixed KISS port [Default: Not set (-1)]</description></item>
		/// </list>
		/// </param>
		public KissChannel(IDictionary<string,string> properties)
			: base(properties)
		{
			if (!properties.TryGetValue("ComPort", out this.comPort))
				this.comPort = "COM1";
			if (String.IsNullOrEmpty(this.comPort))
				throw new InvalidPropertyException("ComPort");
			string _baudrate;
			if (!properties.TryGetValue("Baudrate", out _baudrate))
				_baudrate = "9600";
			try {
				this.baudrate = Int32.Parse(_baudrate);
				if (this.baudrate <= 0)
					throw new ArgumentOutOfRangeException("BaudRate");
			} catch (Exception ex) {
				throw new InvalidPropertyException("Baudrate", ex);
			}
			string _port;
			if (!properties.TryGetValue("Port", out _port))
				_port = "-1";
			try {
				this.kissPort = Int32.Parse(_port);
				if ((this.kissPort < -1) && (this.kissPort > 15))
					throw new ArgumentOutOfRangeException("Port");
			} catch (Exception ex) {
				throw new InvalidPropertyException("Port", ex);
			}
		}

		/// <summary>
		/// Forward a frame to the channel.
		/// </summary>
		/// <param name="frame">The frame to send</param>
		/// <returns>Frame number that identifies the frame for a later reference.
		/// The number is rather long, however wrap around is not impossible.</returns>
		public override UInt64 ForwardFrame(L2Frame frame)
		{
			return base.ForwardFrame(frame);
		}

		/// <summary>
		/// Write data to the output media.
		/// </summary>
		/// <param name="frame">Frame to write.</param>
		protected override void OnForward(L2Frame frame)
		{
			try {
				/*
				if (m_runtime.LogLevel >= L2LogLevel.DEBUG)
					foreach (KeyValuePair<string,string> kvp in frame.addr)
						m_runtime.Log(L2LogLevel.DEBUG, m_name, String.Format("TX# \"{0}\" = \"{1}\"", kvp.Key, kvp.Value));
				*/
				base.OnForward(frame);
				int port;
				if (kissPort >= 0) {
					port = kissPort;
				} else {
					string _port;
					if (!frame.properties.TryGetValue("Port", out _port))
						throw new MissingPropertyException("Port");
					if ("Raw".Equals(_port)) { // This is a raw packet:
						this.port.Write(frame.data, 0, frame.data.Length);
						return;
					}
					// Regulary packet: Add port information:
					port = Int32.Parse(_port);
					if ((port < 0) || (port > 15))
						throw new ArgumentOutOfRangeException("KISS port not valid: " + port);
				}
				byte[] octets = Encode(port, frame.data);
				this.port.Write(octets, 0, octets.Length);
			} catch (Exception ex) {
				this.OnForwardError("Error transmitting to serial port " + this.comPort, ex);
			}
		}

		private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
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
			} catch (IOException ex) {
				this.OnReceiveError("IO Error on port " + this.comPort, ex);
			} catch (InvalidOperationException ex) {
				this.OnReceiveError("COM port is closed: " + this.comPort, ex);
			}
		}

		private void ErrorReceivedHandler(object sender, SerialErrorReceivedEventArgs e)
		{
			//SerialPort sp = (SerialPort)sender;
			switch (e.EventType) {
			case SerialError.TXFull:
				this.OnForwardError("Transmit queue is full on COM port " + this.comPort);
				break;
			case SerialError.RXOver:
				// RX buffer full. Cancel current frame and re-sync:
				this.OnReceiveError("Receive queue is full on COM port " + this.comPort);
				this.iFrameBuffer = 0;
				this.fInFrame = false;
				break;
			case SerialError.Overrun:
				// RX overrun. Cancel current frame and re-sync:
				this.OnReceiveError("Receiver overrun on COM port " + this.comPort);
				this.iFrameBuffer = 0;
				this.fInFrame = false;
				break;
			case SerialError.RXParity:
				// RX parity error. Cancel current frame and re-sync:
				this.OnReceiveError("Parity error on COM port " + this.comPort);
				this.iFrameBuffer = 0;
				this.fInFrame = false;
				break;
			case SerialError.Frame:
				// RX framing error. Cancel current frame and re-sync:
				this.OnReceiveError("Framing error on COM port " + this.comPort);
				this.iFrameBuffer = 0;
				this.fInFrame = false;
				break;
			} // end switch //
		}

		private void HandleRxByte(byte b)
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
						this.OnReceiveError("Invalid octet after FESC on COM port " + this.comPort);
						this.fInFrame = false;
						break;
					} // end switch //
				} else { // Not escape //
					switch (b) {
					case FEND :
						if (this.iFrameBuffer > 0) {
							byte c = rxFrameBuffer[0];
							if ((c & 0x0f) != 0x00) { // Not data frame.
								if (m_runtime.LogLevel >= LogLevel.INFO) {
									byte[] octets = new byte[iFrameBuffer];
									Array.Copy(rxFrameBuffer, 0, octets, 0, iFrameBuffer);
									m_runtime.Log(LogLevel.INFO, m_name,
									              "Ignored incoming TNC control sequence " +
									              HexConverter.ToHexString(octets));
								}
							} else { // Normal data frame:
								int port = c;
								port = port >> 4;
								byte[] octets = new Byte[iFrameBuffer-1];
								Array.Copy(rxFrameBuffer, 1, octets, 0, iFrameBuffer-1);
								L2Frame frame = new L2Frame(m_runtime.NewFrameNo(), false, octets);
								frame.properties.Add("Port", String.Format("{0}", port));
								frame.properties.Add("Channel", m_name);
								this.OnReceive(frame);
							}
							this.iFrameBuffer = 0;
							this.fInFrame = true;
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

		private void AppendRxByte(byte b)
		{
			if (this.iFrameBuffer >= this.rxFrameBuffer.Length) {
				byte[] newRxFrameBuffer = new byte[((this.iFrameBuffer + 1) / FRAME_BUFFER_CHUNK + 1) *
					FRAME_BUFFER_CHUNK];
				Array.Copy (this.rxFrameBuffer, newRxFrameBuffer, this.iFrameBuffer);
				this.rxFrameBuffer = newRxFrameBuffer;
			}
			this.rxFrameBuffer [this.iFrameBuffer++] = b;
			unchecked {
				this.m_rxTotal += 1;
			}
		}

		/// <summary>
		/// Open the channel, so that data actually be transmitted and received.
		/// </summary>
		public override void Open()
		{
			lock(this) {
				this.fInFrame = false;
				this.fEscape = false;
				this.iFrameBuffer = 0;
				this.port = new SerialPort(comPort, this.baudrate);
				this.port.Parity = Parity.None;
				this.port.StopBits = StopBits.One;
				this.port.DataBits = 8;
				this.port.Handshake = Handshake.None;
				this.port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
				this.port.ErrorReceived += new SerialErrorReceivedEventHandler(ErrorReceivedHandler);
				this.port.ReadBufferSize = 256;
				this.port.ReadTimeout = 200;
				this.port.ReceivedBytesThreshold = 1;
				this.port.WriteTimeout = 10000;
				this.port.WriteBufferSize = 256;
				this.port.DiscardNull = false;
				this.port.Open();
				base.Open();
			}
		}

		/// <summary>
		/// Close the channel. No data will be transmitted or received. All queued
		/// data is preserved.
		/// </summary>
		public override void Close()
		{
			lock(this) {
				base.Close();
				if (this.port != null) {
					this.port.Close();
					this.port.Dispose();
					this.port = null;
				}
			}
		}
		
		/*
		protected override void OnReceive(L2Frame frame)
		{
			if (m_runtime.LogLevel >= L2LogLevel.DEBUG)
				foreach (KeyValuePair<string,string> kvp in frame.addr)
					m_runtime.Log(L2LogLevel.DEBUG, m_name, String.Format("RX# \"{0}\" = \"{1}\"", kvp.Key, kvp.Value));
			base.OnReceive(frame);
		}
		*/

		private byte[] Encode(int port, byte[] octets)
		{
			byte[] result;
			lock (this.txFrameBuffer) {
				int j = EncodeAdd(0, FEND);
				byte c;
				unchecked {
					c = (byte) ((port & 0x0f) << 4);
				}
				if (c != FEND) {
					j = EncodeAdd(j, c);
				} else {
					j = EncodeAdd(j, FESC);
					j = EncodeAdd(j, TFEND);
				}
				foreach (byte b in octets) {
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

		private int EncodeAdd(int j, byte b)
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
		
		/// <summary>
		/// Local dispose.
		/// </summary>
		/// <param name="intern">Set to <c>true</c> when calling from user code.</param>
		protected override void Dispose(bool intern)
		{
			if (intern) {
				base.Dispose(true);
				if (this.port != null)
					this.port.Dispose();
			}
		}
		
	}
}

