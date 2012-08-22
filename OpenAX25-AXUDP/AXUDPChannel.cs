//
// AXUDPChannel.cs
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
using System.Net;
using System.Net.Sockets;

using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25AXUDP
{
	/// <summary>
	/// This is a AXUDP channel.
	/// </summary>
	public class AXUDPChannel : L2Channel
	{
		private enum Mode {
			ClientWithRemoteHost, ClientWithoutRemoteHost,
			ServerWithRemoteHost, ServerWithoutRemoteHost };
		private string m_host;
		private int m_port;
		private UdpClient m_udpClient;
		private Mode m_mode;
		
		private static IDictionary<string,IPAddress> ip_map = new Dictionary<string,IPAddress>();

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="properties">Properties of the channel.
		/// <list type="bullet">
		///   <listheader><term>Property name</term><description>Description</description></listheader>
		///   <item><term>Name</term><description>Name of the interface [mandatory]</description></item>
		///   <item><term>Target</term><description>Where to route packages to [Default: ROUTER]</description></item>
		///   <item><term>Host</term><description>Name of the remote host [default: Any]</description></item>
		///   <item><term>Port</term><description>Port [default: 9300]</description></item>
		///   <item><term>Mode</term><description><c>Client<c/c> or <c>Server</c> [default: Client]</description></item>
		/// </list>
		/// </param>
		public AXUDPChannel(IDictionary<string,string> properties)
			: base(properties)
		{
			if (!properties.TryGetValue("Host", out m_host))
				m_host = "Any";
			
			string _port;
			if (!properties.TryGetValue("Port", out _port))
				_port = "9300";
			try {
				m_port = Int32.Parse(_port);
				if (m_port <= 0)
					throw new ArgumentOutOfRangeException("Port");
			} catch (Exception ex) {
				throw new L2InvalidPropertyException("Port (Greater than 0): " + _port, ex);
			}
			
			bool any = ("Any".Equals(m_host));
			string _mode;
			if (!properties.TryGetValue("Mode", out _mode))
				_mode = "Client";
			try {
				if ("Client".Equals(_mode))
					m_mode = (any)?Mode.ClientWithoutRemoteHost:Mode.ClientWithRemoteHost;
				else if ("Server".Equals(_mode))
					m_mode = (any)?Mode.ServerWithoutRemoteHost:Mode.ServerWithRemoteHost;
				else
					throw new ArgumentOutOfRangeException("Mode");
			} catch (Exception ex) {
				throw new L2InvalidPropertyException("Mode (Client|Server): " + _mode, ex);
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
				base.OnForward(frame);
				// Add crc to data block:
				byte[] fcs = BitConverter.GetBytes(CrcB.ComputeCrc(frame.data));
				int size = frame.data.Length;
				byte[] octets = new byte[size+2];
				Array.Copy(frame.data, 0, octets, 0, size);
				Array.Copy(fcs, 0, octets, size, 2);

				// Send data:
				switch (m_mode) {
				case Mode.ClientWithoutRemoteHost :
				case Mode.ServerWithoutRemoteHost :
				case Mode.ServerWithRemoteHost :
					{
						// Get destination properties, if any:
						string host;
						if (!frame.addr.TryGetValue("Host", out host))
							host = m_host;
						int port;
						string _port;
						if (frame.addr.TryGetValue("Port", out _port)) {
							try {
								port = Int32.Parse(_port);
							} catch (Exception ex) {
								m_runtime.Log(L2LogLevel.WARNING, m_name,
								              "Invalid port number ignored " +
								              _port + ": " + ex.Message);
								port = m_port;
							}
						} else {
							port = m_port;
						}
						IPEndPoint ipe = new IPEndPoint(GetIPAddress(host), port);
						m_udpClient.Send(octets, octets.Length, ipe);
					}
					break;
				case Mode.ClientWithRemoteHost :
					{
						m_udpClient.Send(octets, octets.Length);
					}
					break;
				} // end switch //
				m_udpClient.Send(octets, octets.Length);
			} catch (Exception ex) {
				this.OnForwardError("Error transmitting to UDP port", ex);
			}
		}

		/// <summary>
		/// Open the channel, so that data actually be transmitted and received.
		/// </summary>
		public override void Open()
		{
			lock(this) {
                try
                {
                    switch (m_mode)
                    {
					    case Mode.ClientWithRemoteHost :
					    case Mode.ServerWithRemoteHost :
						    m_udpClient = new UdpClient(m_host, m_port);
						    break;
					    case Mode.ClientWithoutRemoteHost :
					    case Mode.ServerWithoutRemoteHost :
						    m_udpClient = new UdpClient(m_port);
						    break;
				    } // end switch //
				    base.Open();
					m_udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), this);
				} catch (Exception ex) {
                    string msg = String.Format(
                        "Error starting receive (host={0},port={1}): {2}",
                            (m_host != null)?m_host:"<NULL>", m_port, ex.Message);
					OnReceiveError(msg);
                    throw new Exception(msg, ex);
				}
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
				if (m_udpClient != null) {
					m_udpClient.Close();
					m_udpClient = null;
				}
			}
		}
		
		/// <summary>
		/// Local dispose.
		/// </summary>
		/// <param name="intern">Set to <c>true</c> when calling from user code.</param>
		protected override void Dispose(bool intern)
		{
			if (intern) {
				base.Dispose(true);
				if (m_udpClient != null)
					m_udpClient.Close();
			}
		}
		
		private void OnReceiveCallback(IAsyncResult ar)
		{
			byte[] octets;
			string remote_host;
			int remote_port;
			try {
				IPEndPoint localEndpoint = (IPEndPoint)m_udpClient.Client.LocalEndPoint;
				IPEndPoint remoteEndPoint = (IPEndPoint)m_udpClient.Client.RemoteEndPoint;
				octets = m_udpClient.EndReceive(ar, ref localEndpoint);
				remote_host = remoteEndPoint.Address.ToString();
				remote_port = remoteEndPoint.Port;
			} catch (Exception ex) {
				OnReceiveError("IO error when reading from port", ex);
				return;
			}
			int size = octets.Length;
			// Check CRC:
			if (size < 2) {
				OnReceiveError("Short package received: ", octets);
				return;
			}
			// Split data / crc and check CRC:
			byte[] data = new byte[size-2];
			Array.Copy(octets, 0, data, 0, size-2);
			byte[] rx_crc = new byte[2];
			Array.Copy(octets, size-2, rx_crc, 0, 2);
			byte[] my_crc = BitConverter.GetBytes(CrcB.ComputeCrc(data));
			if ((rx_crc[0] != my_crc[0]) || (rx_crc[1] != my_crc[1])) {
				OnReceiveError("CRC error: ", octets);
				return;
			}
			// All right, this is a package we can consider:
			L2Frame frame = new L2Frame(m_runtime.NewFrameNo(), false, data);
			frame.addr.Add("Channel", m_name);
			frame.addr.Add("Host", remote_host);
			frame.addr.Add("Port", String.Format("{0}", remote_port));
			OnReceive(frame);
		}
		
		/// <summary>
		/// Receiver callback for UDP data.
		/// </summary>
		/// <param name="ar">AsyncResult for callback.</param>
		public static void ReceiveCallback(IAsyncResult ar)
		{
			AXUDPChannel channel = (AXUDPChannel)(ar.AsyncState);
			channel.OnReceiveCallback(ar);
			try {
				channel.m_udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), channel);
			} catch (Exception ex) {
				channel.OnReceiveError("Error starting receive", ex);
			}
		}
		
		private static IPAddress GetIPAddress(string host)
		{
			IPAddress ipa;
			if (!ip_map.TryGetValue(host, out ipa)) {
				lock (ip_map) {
					// Try it a second time to see, if perhaps another thread resolved this
					// in the meantime.
					if (!ip_map.TryGetValue(host, out ipa)) {
						// Well, no. We have to do it by ourself:
						IPAddress[] ipas = Dns.GetHostAddresses(host);
						if (ipas.Length == 0)
							ipa = null;
						else
							ipa = ipas[0];
						ip_map.Add(host, ipa);
					}
				} // end lock //
			}
			return ipa;
		}
		
	}
}

