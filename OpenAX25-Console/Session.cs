using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25_Console
{
    internal class Session : L3Channel
    {
        internal readonly ConsoleChannel m_channel;
        internal readonly int m_id;
        internal readonly Socket m_socket;
        internal readonly byte[] m_buffer1;
        internal readonly byte[] m_buffer2;

        internal int m_buffer2size = 0;

        private enum SessionState { DISCONNECTED, WAIT_FOR_CONNECT, CONNECTED, WAIT_FOR_DISCONNECT };
        private volatile SessionState m_state = SessionState.DISCONNECTED;
        private volatile bool m_active = false;
        private Object m_stateLock = new Object();
        private IL3Channel m_tx = null;

        internal Session(ConsoleChannel channel, Socket socket)
            : base(channel.Properties, null, true)
        {
            m_channel = channel;
            m_socket = socket;
            m_id = ((IPEndPoint)socket.RemoteEndPoint).Port;
            m_buffer1 = new byte[channel.m_buffer1alloc];
            m_buffer2 = new byte[channel.m_buffer2alloc];
        }

        public override void Open()
        {
            try
            {
                lock (m_stateLock)
                {
                    if (m_state == SessionState.CONNECTED)
                        return;
                    m_runtime.Log(LogLevel.INFO, m_channel.Name, "Open session: " + m_id);
                    base.Open();
                    m_tx = m_channel.m_localEndpoint.Bind(this, Properties, String.Format("Console({0})", m_id));
                    if (m_tx == null)
                        throw new Exception("Unable to bind");
                    m_buffer2size = 0;
                    m_state = SessionState.WAIT_FOR_CONNECT;
                    m_active = true;
                    m_tx.Send(new DL_CONNECT_Request(m_channel.m_remoteAddr, AX25Version.V2_0));
                    while (m_state == SessionState.WAIT_FOR_CONNECT)
                        Monitor.Wait(m_stateLock);
                    if (m_state != SessionState.CONNECTED)
                        return;
                    ConsoleChannel.ContinueRead(this);
                }
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.ERROR,
                    "Console session " + m_id, "Exception in \"Open()\": " + e.Message);
            }
        }

        public override void Close()
        {
            try
            {
                m_runtime.Log(LogLevel.INFO, m_channel.Name, "Close session: " + m_id);
                lock (m_stateLock)
                {
                    m_active = false;
                    if ((m_tx != null) && (m_state != SessionState.DISCONNECTED))
                    {
                        m_tx.Send(new DL_DISCONNECT_Request());
                        m_channel.m_localEndpoint.Unbind(m_tx);
                        m_tx = null;
                    }
                    m_state = SessionState.DISCONNECTED;
                }
                m_channel.Close(this);
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.ERROR,
                    "TCPServer session " + m_id, "Spurious exceptionin \"Close()\": " + e.Message);
            }
        }

        internal bool Receive(int nReceived)
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
            {
                byte[] data = new byte[nReceived];
                m_runtime.Log(LogLevel.DEBUG, m_channel.Name,
                    "Received on channel " + m_id + ": " + HexConverter.ToHexString(data, true));
            }

            try
            {
                lock (m_buffer2)
                {
                    // In text mode look for CR of NL. If found, force output later.
                    bool forceOutput = false;
                    for (int i = 0; (i < nReceived) && !forceOutput; ++i)
                        switch (m_buffer1[i]) { case 0x0d: case 0x0a: forceOutput = true; break; default: break; }

                    // Test if received data will fit into buffer2:
                    int new_buffer2size = m_buffer2size + nReceived;

                    // If it fits, simply copy data to buffer2:
                    if (new_buffer2size <= m_channel.m_buffer2alloc)
                    {
                        Array.Copy(m_buffer1, 0, m_buffer2, m_buffer2size, nReceived);
                        m_buffer2size = new_buffer2size;
                    }
                    // If it not fits at a whole, do it chunk by chunk:
                    else
                    {
                        int iReceived = 0;
                        while (iReceived < nReceived)
                        {
                            int nToCopy = Math.Min(m_channel.m_buffer2alloc - m_buffer2size, nReceived - iReceived);
                            Array.Copy(m_buffer1, iReceived, m_buffer2, m_buffer2size, nToCopy);
                            m_buffer2size += nToCopy;
                            Send(m_buffer2, m_buffer2size);
                            m_buffer2size = 0;
                            iReceived += nToCopy;
                        } // end while //
                    }

                    // If output was forced and there is data to output, output it now:
                    if (forceOutput && (m_buffer2size > 0))
                    {
                        Send(m_buffer2, m_buffer2size);
                        m_buffer2size = 0;
                    }

                    // Allow more input from the channel:
                    ConsoleChannel.ContinueRead(this);
                } // end lock //
                return true;
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.ERROR, m_channel.Name, e.Message);
                m_buffer2size = 0;
                return false;
            }
        }

        protected override void Input(DataLinkPrimitive p, bool expedited)
        {
            if (!m_active)
                return;
            bool close = false;
            lock (m_stateLock)
            {
                switch (p.DataLinkPrimitiveType)
                {
                    case DataLinkPrimitive_T.DL_CONNECT_Confirm_T :
                        m_state = SessionState.CONNECTED;
                        Monitor.PulseAll(m_stateLock);
                        break;
                    case DataLinkPrimitive_T.DL_CONNECT_Indication_T :
                        m_state = SessionState.CONNECTED;
                        Monitor.PulseAll(m_stateLock);
                        break;
                    case DataLinkPrimitive_T.DL_DATA_Indication_T :
                        ConsoleChannel.Send(this, ((DL_DATA_Indication)p).Data, false);
                        break;
                    case DataLinkPrimitive_T.DL_UNIT_DATA_Indication_T :
                        ConsoleChannel.Send(this, ((DL_UNIT_DATA_Indication)p).Data, true);
                        break;
                    case DataLinkPrimitive_T.DL_DISCONNECT_Confirm_T :
                        m_state = SessionState.DISCONNECTED;
                        Monitor.PulseAll(m_stateLock);
                        close = true;
                        break;
                    case DataLinkPrimitive_T.DL_DISCONNECT_Indication_T:
                        m_state = SessionState.DISCONNECTED;
                        Monitor.PulseAll(m_stateLock);
                        close = true;
                        break;
                    default :
                        m_runtime.Log(LogLevel.WARNING, m_name,
                            "Received unexpected primitive: " + p.DataLinkPrimitiveTypeName);
                        break;
                } // end switch //
            } // end lock //
            if (close)
                Close();
        }

        private void Send(byte[] data, int length)
        {
            if (!m_active)
                return;
            if (length == 0)
                return;
            bool isControl = (data[0] == 0x1B); // ESC: Very simple terminal protocol ;-)
            lock (m_stateLock)
            {
                if (m_state == SessionState.CONNECTED)
                {
                    if (isControl)
                    {
                        byte[] frame = new byte[length - 1];
                        Array.Copy(data, 1, frame, 0, length - 1);
                        m_tx.Send(new DL_UNIT_DATA_Request(frame));
                    }
                    else
                    {
                        byte[] frame = new byte[length];
                        Array.Copy(data, 0, frame, 0, length);
                        m_tx.Send(new DL_DATA_Request(frame));
                    }
                }
            }
        }

    }

}
