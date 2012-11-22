//
// TCPServerChannel.cs
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
using System.Linq;
using System.Text;
using OpenAX25Core;
using OpenAX25Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OpenAX25_TCPServer
{
    public class TCPServerChannel : L3Channel
    {

        internal readonly int m_buffer1alloc;
        internal readonly int m_buffer2alloc;
        internal readonly long m_timerValue;
        internal readonly bool m_binary;
        internal readonly string m_localAddr;
        internal readonly string m_remoteAddr;
        internal readonly AX25Version m_version;

        private readonly int m_port;
        private Thread m_thread = null;
        private ManualResetEvent m_allDone = new ManualResetEvent(false);
        private Socket m_listener;
        private ILocalEndpoint m_localEndpoint = null;
        private IL3Channel m_transmitter = null;
        private enum ConnectionState { DISCONNECTED, WAITING_FOR_CONNECT, CONNECTED, WAITING_FOR_DISCONNECT };
        private volatile ConnectionState m_state = ConnectionState.DISCONNECTED;
        private Object m_stateLock = new Object();
        private volatile int m_errors = 0;
        private volatile Exception m_exception = null;

        /// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="properties">Properties of the channel.
		/// <list type="bullet">
		///   <listheader><term>Property name</term><description>Description</description></listheader>
		///   <item><term>Name</term><description>Name of the interface [mandatory]</description></item>
        ///   <item><term>LocalAddr</term><description>Local link address [mandatory]</description></item>
        ///   <item><term>RemoteAddr</term><description>Remote link address [mandatory]</description></item>
        ///   <item><term>AX25Version</term><description>AX.25 version to use (2.0|2.2) [default: 2.0]</description></item>
        ///   <item><term>Target</term><description>Where to route packages to [Default: PROTO]</description></item>
		///   <item><term>Port</term><description>Port [default: 9300]</description></item>
        ///   <item><term>Timer</term><description>Send timer time [ms] [default: disabled]</description></item>
        ///   <item><term>Binary</term><description>Binary mode [default: false]</description></item>
        ///   <item><term>B1_Size</term><description>Size of input buffer 1 [default: 8192]</description></item>
        ///   <item><term>B2_Size</term><description>Size of input buffer 2 [default: 8192]</description></item>
        ///   <item><term>SRT</term><description>Initial smoothed round trip time in ms [Default: 3000]</description></item>
        ///   <item><term>SAT</term><description>Ínitial smoothed activity timer in ms [Default: 10000]</description></item>
        ///   <item><term>N1</term><description>Ínitial maximum number of octets in the information field of a frame [Default: 255]</description></item>
        ///   <item><term>N2</term><description>Ínitial maximum number of retires permitted [Default: 16]</description></item>
        /// </list>
		/// </param>
        public TCPServerChannel(IDictionary<string, string> properties)
			: base(properties)
		{
			string _v;

            if (!(m_target is IL3DataLinkProvider))
                throw new InvalidPropertyException("Target is not Layer 3 Data Link provider");

            if (!properties.TryGetValue("LocalAddr", out m_localAddr))
                throw new MissingPropertyException("LocalAddr");

			if (!properties.TryGetValue("Port", out _v))
				_v = "9300";
			try {
				m_port = Int32.Parse(_v);
				if (m_port <= 0)
					throw new ArgumentOutOfRangeException("[1..MAXINT]");
			} catch (Exception ex) {
				throw new InvalidPropertyException("Port (Greater than 0): " + _v, ex);
			}

            if (!properties.TryGetValue("RemoteAddr", out m_remoteAddr))
                throw new MissingPropertyException("RemoteAddr");

            if (!properties.TryGetValue("AX25Version", out _v))
                _v = "2.0";
            try
            {
                if ("2.0".Equals(_v))
                    m_version = AX25Version.V2_0;
                else if ("2.2".Equals(_v))
                    m_version = AX25Version.V2_2;
                else
                    throw new ArgumentOutOfRangeException("2.0|2.2");
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("AX25Version: " + _v, ex);
            }

            if (!properties.TryGetValue("Port", out _v))
                _v = "9300";
            try
            {
                m_port = Int32.Parse(_v);
                if (m_port <= 0)
                    throw new ArgumentOutOfRangeException("[1..MAXINT]");
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("AX25Version: " + _v, ex);
            }

            if (!properties.TryGetValue("Timer", out _v))
                _v = "0";
            try
            {
                m_timerValue = Int64.Parse(_v);
                if (m_timerValue < 0)
                    throw new ArgumentOutOfRangeException("[0..MAXLONG]");
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("Timer (Greater than 0): " + _v, ex);
            }

            if (!properties.TryGetValue("Binary", out _v))
                _v = "false";
            try
            {
                m_binary = Boolean.Parse(_v);
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("Binary: " + _v, ex);
            }

            if (!properties.TryGetValue("B1_Size", out _v))
                _v = "8192";
            try
            {
                m_buffer1alloc = Int32.Parse(_v);
                if (m_buffer1alloc < 1)
                    throw new ArgumentOutOfRangeException("[1..MAXINT]");
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("B1_Size: " + _v, ex);
            }

            if (!properties.TryGetValue("B2_Size", out _v))
                _v = "8192";
            try
            {
                m_buffer2alloc = Int32.Parse(_v);
                if (m_buffer2alloc < 1)
                    throw new ArgumentOutOfRangeException("[1..MAXINT]");
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException("B2_Size: " + _v, ex);
            }

        }

        /// <summary>
        /// Open the channel, so that data actually be transmitted and received.
        /// </summary>
        public override void Open()
        {
            if (m_thread != null) // Thread is already open.
                return;
            lock (this)
            {
                try
                {
                    base.Open();
                    m_localEndpoint = ((IL3DataLinkProvider)m_target).RegisterLocalEndpoint(m_localAddr, Properties);
                    m_transmitter = m_localEndpoint.Bind(this, Properties, m_name + "." + m_remoteAddr);
                    m_thread = new Thread(new ThreadStart(ServiceRun));
                    m_thread.Start();
                }
                catch (Exception e)
                {
                    m_runtime.Log(LogLevel.ERROR, m_name, "Unable to open channel: " + e.Message);
                    m_thread = null;
                    if (m_transmitter != null)
                    {
                        m_localEndpoint.Unbind(m_transmitter);
                        m_transmitter = null;
                    }
                    if (m_localEndpoint != null)
                    {
                        ((IL3DataLinkProvider)m_target).UnregisterLocalEndpoint(m_localEndpoint);
                        m_localEndpoint = null;
                    }
                }
            }
        }

        /// <summary>
        /// Close the channel. No data will be transmitted or received. All queued
        /// data is preserved.
        /// </summary>
        public override void Close()
        {
            if (m_thread == null) // Thread is already closed.
                return;
            lock(this) {
                try
                {
                    m_thread.Abort();
                    m_thread.Join();
                    if (m_localEndpoint != null)
                    {
                        if (m_transmitter != null)
                            m_localEndpoint.Unbind(m_transmitter);
                        m_transmitter = null;
                        ((IL3DataLinkProvider)m_target).UnregisterLocalEndpoint(m_localEndpoint);
                        m_localEndpoint = null;
                    }
                }
                catch (Exception e)
                {
                    m_runtime.Log(LogLevel.ERROR, m_name, "Unable to close channel: " + e.Message);
                }
                finally
                {
                    m_thread = null;
                }
            } // end lock //
            base.Close();
        }

        private void ServiceRun()
        {
            // Establish the local endpoint for the socket:
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, m_port);

            // Create a TCP/IP socket:
            m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections:
            try
            {
                m_listener.Bind(localEndPoint);
                m_listener.Listen(100);

                while (true)
                {
                    try
                    {
                        // Set the evne to nonsignalled state:
                        m_allDone.Reset();
                        m_runtime.Log(LogLevel.INFO, m_name, "Waiting for incoming connection ...");
                        m_listener.BeginAccept(new AsyncCallback(AcceptCallback), this);

                        // Wait until a connection is made before continuing;
                        m_allDone.WaitOne();
                    }
                    catch (Exception e)
                    {
                        m_runtime.Log(LogLevel.ERROR, m_name,
                            "Error in consumer thread: " + e.Message);
                    }
                } // end while //
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.ERROR, m_name, e.Message);
            }
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                TCPServerChannel channel = (TCPServerChannel)ar.AsyncState;
                // Signal the main thread to continue:
                channel.m_allDone.Set();

                // Get the socket that handles the client request:
                Socket handler = channel.m_listener.EndAccept(ar);

                // Create the state object:
                Session session = new Session(channel, handler);
                session.Open();
            }
            catch (Exception e) {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServerChannel", "Spurios exception in \"AcceptCallback\": "
                        + e.Message);
            }
        }

        private static void ReadCallback(IAsyncResult ar)
        {
            try
            {
                Session session = (Session)ar.AsyncState;
                Socket socket = session.m_socket;
                int bytesRead = socket.EndReceive(ar);
                if ((bytesRead == 0) || !session.Receive(bytesRead))
                {
                    session.Close();
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServerChannel", "Spurios exception in \"ReadCallback\": "
                        + e.Message);
            }
        }

        internal static void ContinueRead(Session session)
        {
            try
            {
                if (session.m_dead)
                    return;
                Socket handler = session.m_socket;
                handler.BeginReceive(session.m_buffer1, 0, session.m_channel.m_buffer1alloc, 0,
                    new AsyncCallback(ReadCallback), session);
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServerChannel", "Spurios exception in \"ContinueRead\": "
                        + e.Message);
            }
        }

        internal static void Send(Session session, byte[] data, int length)
        {
            try
            {
                if (session.m_dead)
                    return;
                session.m_socket.BeginSend(data, 0, length, 0,
                    new AsyncCallback(SendCallback), session);
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServerChannel", "Spurios exception in \"Send\": "
                        + e.Message);
            }
        }

        internal void Put(byte[] data)
        {
            lock (m_stateLock)
            {
                Connect();
                DL_DATA_Request rq = new DL_DATA_Request(data);
                m_transmitter.Send(rq);
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                Session session = (Session)ar.AsyncState;
                if (session.m_dead)
                    return;
                /* int bytesSent = */ session.m_socket.EndSend(ar);
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServerChannel", "Spurios exception in \"SendCallback\": "
                        + e.Message);
            }
        }

        private void Connect()
        {
            lock (m_stateLock)
            {
                if (m_state == ConnectionState.CONNECTED)
                    return;
                int errors = m_errors;
                if (m_state != ConnectionState.WAITING_FOR_CONNECT)
                {
                    DL_CONNECT_Request rq = new DL_CONNECT_Request(m_remoteAddr, m_version);
                    m_transmitter.Send(rq);
                    m_state = ConnectionState.WAITING_FOR_CONNECT;
                }
                do
                {
                    Monitor.Wait(m_stateLock);
                    if (m_errors != errors)
                        throw (m_exception != null) ? m_exception : new Exception("Unable to connect");
                } while (m_state != ConnectionState.CONNECTED);
            } // end Lock //
        }

        private void Disconnect()
        {
            try
            {
                lock (m_stateLock)
                {
                    if (m_state == ConnectionState.DISCONNECTED)
                        return;
                    int errors = m_errors;
                    if (m_state != ConnectionState.WAITING_FOR_DISCONNECT)
                    {
                        DL_DISCONNECT_Request rq = new DL_DISCONNECT_Request();
                        m_transmitter.Send(rq);
                        m_state = ConnectionState.WAITING_FOR_DISCONNECT;
                    }
                    do
                    {
                        Monitor.Wait(m_stateLock);
                        if (m_errors != errors)
                            throw (m_exception != null) ? m_exception : new Exception("Unable to disconnect");
                    } while (m_state != ConnectionState.DISCONNECTED);
                } // end lock //
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServerChannel", "Spurios exception in \"Disconnect\": "
                        + e.Message);
            }
        }

        /// <summary>
        /// Method to process input message.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="p">The message to process.</param>
        /// <param name="expedited">Send expedited if set.</param>
        protected override void Input(DataLinkPrimitive p, bool expedited = false)
        {
            try
            {
                lock (this)
                {
                    if (p == null)
                        throw new ArgumentNullException("p");
                    switch (p.DataLinkPrimitiveType)
                    {
                        case DataLinkPrimitive_T.DL_CONNECT_Confirm_T:
                            OnConnectConfirm();
                            break;
                        case DataLinkPrimitive_T.DL_DISCONNECT_Confirm_T:
                            OnDisconnectConfirm();
                            break;
                        case DataLinkPrimitive_T.DL_DISCONNECT_Indication_T:
                            OnDisconnectIndication();
                            break;
                        case DataLinkPrimitive_T.DL_DATA_Indication_T:
                            OnDataIndication(((DL_DATA_Indication)p).Data);
                            break;
                        case DataLinkPrimitive_T.DL_ERROR_Indication_T:
                            OnErrorIndication(((DL_ERROR_Indication)p).ErrorCode,
                                ((DL_ERROR_Indication)p).Description);
                            break;
                        default:
                            m_runtime.Log(LogLevel.WARNING, m_name,
                                "Dropping unexpected primitive: " + p.DataLinkPrimitiveTypeName);
                            break;
                    } // end switch //
                } // end lock //
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServerChannel", "Spurios exception in \"Input\": "
                        + e.Message);
            }
        }

        private void OnErrorIndication(long erc, string description)
        {
            try
            {
                lock (m_stateLock)
                {
                    m_errors += 1;
                    string message = String.Format("Error {0} received from DataLink: {1}",
                        erc, description);
                    m_runtime.Log(LogLevel.ERROR, m_name, message);
                    m_exception = new Exception(message);
                    Monitor.PulseAll(m_stateLock);
                }
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServerChannel", "Spurios exception in \"OnErrorIndication\": "
                        + e.Message);
            }
        }

        private void OnDataIndication(byte[] p)
        {
            try
            {
                lock (m_stateLock)
                {
                    if (m_state != ConnectionState.CONNECTED)
                    {
                        m_runtime.Log(LogLevel.WARNING, m_name,
                            "Dropping unexpected data: " + HexConverter.ToHexString(p, true));
                        return;
                    }
                    m_runtime.Log(LogLevel.INFO, m_name,
                        "Dropping expected data: " + HexConverter.ToHexString(p, true));
                }
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServerChannel", "Spurios exception in \"OnDataIndication\": "
                        + e.Message);
            }
        }

        private void OnDisconnectIndication()
        {
            try
            {
                lock (m_stateLock)
                {
                    m_state = ConnectionState.DISCONNECTED;
                    Monitor.PulseAll(m_stateLock);
                }
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServerChannel", "Spurios exception in \"OnDisconnectIndication\": "
                        + e.Message);
            }
        }

        private void OnDisconnectConfirm()
        {
            try
            {
                lock (m_stateLock)
                {
                    m_state = ConnectionState.DISCONNECTED;
                    Monitor.PulseAll(m_stateLock);
                }
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServerChannel", "Spurios exception in \"OnDisconnectConfirm\": "
                        + e.Message);
            }
        }

        private void OnConnectConfirm()
        {
            try
            {
                lock (m_stateLock)
                {
                    m_state = ConnectionState.CONNECTED;
                    Monitor.PulseAll(m_stateLock);
                }
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServerChannel", "Spurios exception in \"OnConnectConfirm\": "
                        + e.Message);
            }
        }

    }
}
