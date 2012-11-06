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
        internal readonly string m_callsign;

        private readonly int m_port;
        private Thread m_thread = null;
        private ManualResetEvent m_allDone = new ManualResetEvent(false);
        private Socket m_listener;
        private Guid m_registration = Guid.Empty;

        /// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="properties">Properties of the channel.
		/// <list type="bullet">
		///   <listheader><term>Property name</term><description>Description</description></listheader>
		///   <item><term>Name</term><description>Name of the interface [mandatory]</description></item>
        ///   <item><term>Callsign</term><description>Local callsign [madatory]</description></item>
        ///   <item><term>Target</term><description>Where to route packages to [Default: PROTO]</description></item>
		///   <item><term>Port</term><description>Port [default: 9300]</description></item>
        ///   <item><term>Timer</term><description>Send timer time [ms] [default: disabled]</description></item>
        ///   <item><term>Binary</term><description>Binary mode [default: false]</description></item>
        ///   <item><term>B1_Size</term><description>Size of input buffer 1 [default: 8192]</description></item>
        ///   <item><term>B2_Size</term><description>Size of input buffer 2 [default: 8192]</description></item>
        /// </list>
		/// </param>
        public TCPServerChannel(IDictionary<string, string> properties)
			: base(properties)
		{
			string _v;

            if (!(m_target is IL3DataLinkProvider))
                throw new InvalidPropertyException("Target is not Layer 3 Data Link provider");

            if (!properties.TryGetValue("Callsign", out m_callsign))
                throw new MissingPropertyException("Callsign");

			if (!properties.TryGetValue("Port", out _v))
				_v = "9300";
			try {
				m_port = Int32.Parse(_v);
				if (m_port <= 0)
					throw new ArgumentOutOfRangeException("[1..MAXINT]");
			} catch (Exception ex) {
				throw new InvalidPropertyException("Port (Greater than 0): " + _v, ex);
			}

            if (!properties.TryGetValue("Port", out _v))
                _v = "0";
            try
            {
                m_timerValue = Int64.Parse(_v);
                if (m_timerValue <= 0)
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
                    m_registration = ((IL3DataLinkProvider)m_target).RegisterL3Endpoint(m_callsign, this);
                    m_thread = new Thread(new ThreadStart(L2Run));
                    m_thread.Start();
                }
                catch (Exception e)
                {
                    m_runtime.Log(LogLevel.ERROR, m_name, "Unable to open channel: " + e.Message);
                    m_thread = null;
                    if (m_registration != Guid.Empty)
                    {
                        ((IL3DataLinkProvider)m_target).UnregisterL3Endpoint(m_registration);
                        m_registration = Guid.Empty;
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
                    if (m_registration != Guid.Empty)
                    {
                        ((IL3DataLinkProvider)m_target).UnregisterL3Endpoint(m_registration);
                        m_registration = Guid.Empty;
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
            }
            base.Close();
        }

        private void L2Run()
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
                    // Set the evne to nonsignalled state:
                    m_allDone.Reset();
                    m_runtime.Log(LogLevel.INFO, m_name, "Waiting for incoming connection ...");
                    m_listener.BeginAccept(new AsyncCallback(AcceptCallback), this);

                    // Wait until a connection is made before continuing;
                    m_allDone.WaitOne();
                } // end while //
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.ERROR, m_name, e.Message);
            }
        }

        private static void AcceptCallback(IAsyncResult ar)
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

        private static void ReadCallback(IAsyncResult ar)
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

        internal static void ContinueRead(Session session)
        {
            Socket handler = session.m_socket;
            handler.BeginReceive(session.m_buffer1, 0, session.m_channel.m_buffer1alloc, 0,
                new AsyncCallback(ReadCallback), session);
        }

        internal static void Send(Session session, byte[] data, int length)
        {
            session.m_socket.BeginSend(data, 0, length, 0,
                new AsyncCallback(SendCallback), session);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            Session session = (Session)ar.AsyncState;
            /* int bytesSent = */ session.m_socket.EndSend(ar);
        }

    }
}
