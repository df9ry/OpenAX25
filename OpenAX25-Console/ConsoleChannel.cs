using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenAX25Core;
using OpenAX25Contracts;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace OpenAX25_Console
{
    public class ConsoleChannel : IChannel
    {

        internal readonly string m_localAddr;
        internal readonly string m_remoteAddr;
        internal readonly int m_buffer1alloc;
        internal readonly int m_buffer2alloc;
        internal readonly Runtime m_runtime;
        internal readonly IL3DataLinkProvider m_target;
        internal readonly string m_name;
        internal readonly IDictionary<string, string> m_properties;

        internal ILocalEndpoint m_localEndpoint = null;

        private readonly int m_port;

        private ManualResetEvent m_allDone = new ManualResetEvent(false);
        private Thread m_thread = null;
        private Socket m_listener = null;

        /// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="properties">Properties of the channel.
		/// <list type="bullet">
		///   <listheader><term>Property name</term><description>Description</description></listheader>
		///   <item><term>Name</term><description>Name of the interface [mandatory]</description></item>
        ///   <item><term>Target</term><description>Where to route packages to [Default: PROTO]</description></item>
		///   <item><term>Port</term><description>Port [default: 9000]</description></item>
        ///   <item><term>LocalAddr</term><description>Local link address [default: {GUID}]</description></item>
        ///   <item><term>RemoteAddr</term><description>Remote link address [default: CQ]</description></item>
        ///   <item><term>B1_Size</term><description>Size of input buffer 1 [default: 512]</description></item>
        ///   <item><term>B2_Size</term><description>Size of input buffer 2 [default: 512]</description></item>
        /// </list>
		/// </param>
        public ConsoleChannel(IDictionary<string, string> properties)
		{
            m_properties = properties;
            m_runtime = Runtime.Instance;

            if (!properties.TryGetValue("Name", out m_name))
                throw new InvalidPropertyException("Missing mandatory property \"Name\"");

            string _v;
            if (!properties.TryGetValue("Target", out _v))
                _v = "PROTO";
            IChannel target = m_runtime.LookupChannel(_v);
            if (target == null)
                throw new InvalidPropertyException("Target not found: \"" + _v + "\"");
            if (!(target is IL3DataLinkProvider))
                throw new InvalidPropertyException("Target \"" + _v + "\" is not a Layer 3 Data Link provider");
            m_target = (IL3DataLinkProvider)target;

            if (!properties.TryGetValue("Port", out _v))
				_v = "9000";
			try {
				m_port = Int32.Parse(_v);
				if (m_port <= 0)
					throw new ArgumentOutOfRangeException("[1..MAXINT]");
			} catch (Exception ex) {
				throw new InvalidPropertyException("Port (Greater than 0): " + _v, ex);
			}

            if (!properties.TryGetValue("LocalAddr", out m_localAddr))
                m_localAddr = Guid.NewGuid().ToString();

            if (!properties.TryGetValue("RemoteAddr", out m_remoteAddr))
                m_remoteAddr = "CQ";

            if (!properties.TryGetValue("B1_Size", out _v))
                _v = "512";
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
                _v = "512";
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
        public IDictionary<string, string> Properties { get { return m_properties; } }

        /// <summary>
        /// Open the channel, so that data actually be transmitted and received.
        /// </summary>
        public void Open()
        {
            if (m_thread != null) // Thread is already open.
                return;
            lock (this)
            {
                try
                {
                    m_localEndpoint = ((IL3DataLinkProvider)m_target).RegisterLocalEndpoint(m_localAddr, Properties);
                    m_thread = new Thread(new ThreadStart(ServiceRun));
                    m_thread.Start();
                }
                catch (Exception e)
                {
                    m_runtime.Log(LogLevel.ERROR, m_name, "Unable to open channel: " + e.Message);
                    m_thread = null;
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
        public void Close()
        {
            if (m_thread == null) // Thread is already closed.
                return;
            lock (this)
            {
                try
                {
                    m_thread.Abort();
                    m_thread.Join();
                    if (m_localEndpoint != null)
                    {
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
        }

        /// <summary>
        /// Resets the channel. The data link is closed and reopened. All pending
        /// data is withdrawn.
        /// </summary>
        public void Reset()
        {
            Close();
            Open();
        }

        internal void Close(Session session)
        {
            session.m_socket.Shutdown(SocketShutdown.Both);
            session.m_socket.Close();
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
                ConsoleChannel channel = (ConsoleChannel)ar.AsyncState;
                // Signal the main thread to continue:
                channel.m_allDone.Set();

                // Get the socket that handles the client request:
                Socket handler = channel.m_listener.EndAccept(ar);

                // Create the state object:
                Session session = new Session(channel, handler);
                session.Open();
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "ConsoleChannel", "Spurios exception in \"AcceptCallback\": "
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
                if ((bytesRead == 0) || !session.ReceiveFromLocal(bytesRead))
                    session.Close();
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "ConsoleChannel", "Spurios exception in \"ReadCallback\": "
                        + e.Message);
            }
        }

        internal static void ContinueRead(Session session)
        {
            try
            {
                Socket socket = session.m_socket;
                socket.BeginReceive(session.m_buffer1, 0, session.m_channel.m_buffer1alloc, 0,
                    new AsyncCallback(ReadCallback), session);
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "ConsoleChannel", "Spurios exception in \"ContinueRead\": "
                        + e.Message);
            }
        }

        internal static void SendToLocal(Session session, byte[] data, int length)
        {
            try
            {
                session.m_socket.BeginSend(data, 0, length, 0,
                    new AsyncCallback(SendCallback), session);
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "ConsoleChannel", "Spurios exception in \"Send\": "
                        + e.Message);
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                Session session = (Session)ar.AsyncState;
                /* int bytesSent = */
                session.m_socket.EndSend(ar);
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "ConsoleChannel", "Spurios exception in \"SendCallback\": "
                        + e.Message);
            }
        }

    }
}
