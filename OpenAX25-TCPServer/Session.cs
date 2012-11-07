using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using OpenAX25Core;
using OpenAX25Contracts;
using System.Threading;
using System.Net;

namespace OpenAX25_TCPServer
{
    internal class Session
    {
        internal readonly TCPServerChannel m_channel;
        internal readonly int m_id;
        internal readonly Socket m_socket;
        internal readonly byte[] m_buffer1;
        internal readonly byte[] m_buffer2;
        internal int m_buffer2size = 0;
        internal volatile Boolean m_dead = false;

        private Timer m_timer = null;
        private readonly Runtime m_runtime = Runtime.Instance;
 
        private enum SessionState { DISCONNECTED, WAIT_FOR_CONNECT, CONNECTED, WAIT_FOR_DISCONNECT };
        private volatile SessionState m_state = SessionState.DISCONNECTED;
        private Object m_stateLock = new Object();

        internal Session(TCPServerChannel channel, Socket socket)
        {
            m_channel = channel;
            m_socket = socket;
            m_id = ((IPEndPoint)socket.RemoteEndPoint).Port;
            m_buffer1 = new byte[channel.m_buffer1alloc];
            m_buffer2 = new byte[channel.m_buffer2alloc];
            m_timer = (channel.m_timerValue > 0)?
                new Timer(OnTimerCallback, this, Timeout.Infinite, Timeout.Infinite):null;
        }

        internal void Open()
        {
            try
            {
                m_runtime.Log(LogLevel.INFO, m_channel.Name, "Open session: " + m_id);
                m_buffer2size = 0;
                m_dead = false;
                TCPServerChannel.ContinueRead(this);
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.ERROR,
                    "TCPServer session " + m_id, "Spurious expeptionin \"Open()\": " + e.Message);
            }
        }

        internal bool Receive(int nReceived)
        {
            if (m_dead)
                return false;
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
                    if (!m_channel.m_binary)
                        for (int i = 0; (i < nReceived) && !forceOutput; ++i)
                            switch (m_buffer1[i]) { case 0x0d: case 0x0a: forceOutput = true; break; default: break; }

                    // If the current buffer is empty and timeout is specified force timer start later:
                    bool startTimer = ((m_buffer2size == 0) && (m_timer != null));

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
                            Output(m_buffer2, m_buffer2size);
                            if (m_dead)
                                return false;
                            m_buffer2size = 0;
                            iReceived += nToCopy;
                        } // end while //
                    }

                    // If output was forced and there is data to output, output it now:
                    if (forceOutput && (m_buffer2size > 0) && !m_dead)
                    {
                        Output(m_buffer2, m_buffer2size);
                        m_buffer2size = 0;
                    }

                    if (startTimer && (m_buffer2size > 0) && (m_timer != null))
                    {
                        if (m_runtime.LogLevel >= LogLevel.DEBUG)
                            m_runtime.Log(LogLevel.DEBUG, "TCPServerChannel: " + m_id,
                                "Timer start: " + m_channel.m_timerValue);
                        m_timer.Change(m_channel.m_timerValue, Timeout.Infinite);
                    }

                    // Allow more input from the channel:
                    if (!m_dead)
                        TCPServerChannel.ContinueRead(this);
                } // end lock //
                return true;
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.ERROR, m_channel.Name, e.Message);
                m_buffer2size = 0;
                m_dead = true;
                return false;
            }
        }

        internal void Close()
        {
            try
            {
                if (m_timer != null)
                {
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, "TCPServerChannel: " + m_id,
                            "Timer stop");
                    m_timer.Change(Timeout.Infinite, Timeout.Infinite);
                    m_timer.Dispose();
                    m_timer = null;
                }
                /*
                if (m_buffer2size > 0)
                {
                    Output(m_buffer2, m_buffer2size);
                    m_buffer2size = 0;
                }
                */
                m_dead = true;
                m_runtime.Log(LogLevel.INFO, m_channel.Name, "Close session: " + m_id);
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.ERROR,
                    "TCPServer session " + m_id, "Spurious expeptionin \"Close()\": " + e.Message);
            }
        }

        private void Output(byte[] data, int length)
        {
            if (m_dead)
                return;
            lock (this)
            {
                Connect();
                if (m_dead)
                    return;
                byte[] frame = new byte[length + 5];
                SetHeader(frame);
                Array.Copy(data, 0, frame, 5, length);
                m_channel.Put(frame);
            }
        }

        private static void OnTimerCallback(object obj)
        {
            try
            {
                Session session = (Session)obj;
                lock (session)
                {
                    if (session.m_runtime.LogLevel >= LogLevel.DEBUG)
                        session.m_runtime.Log(LogLevel.DEBUG, "TCPServer session " + session.m_id,
                            "Timer Callback");
                    if (session.m_timer == null)
                        return; // Spurious
                    if (session.m_dead)
                        return;
                    if (session.m_buffer2size > 0)
                        lock (session.m_buffer2)
                        {
                            if (session.m_buffer2size > 0)
                            {
                                session.Output(session.m_buffer2, session.m_buffer2size);
                                session.m_buffer2size = 0;
                                if (session.m_dead)
                                    return;
                            }
                        }
                } // end lock //
            }
            catch (Exception e)
            {
                Runtime.Instance.Log(LogLevel.ERROR,
                    "TCPServer session", "Spurious expeption in \"OnTimerCallback()\": " + e.Message);
            }
        }

        private void Connect()
        {
            if (m_dead)
                return;
            lock (m_stateLock)
            {
                if (m_state == SessionState.CONNECTED)
                    return;
                if (m_state != SessionState.WAIT_FOR_CONNECT)
                {
                    byte[] data = new byte[5];
                    SetHeader(data, 'C');
                    m_channel.Put(data);
                    if (m_dead)
                        return;
                }
                do
                {
                    Monitor.Wait(m_stateLock);
                } while ((!m_dead) && (m_state != SessionState.CONNECTED));
            } // end lock //
        }

        private int SetHeader(byte[] buffer, char cmd = 'I')
        {
            int id = m_id;
            buffer[0] = (byte)(id % 0x100); id = id / 0x100;
            buffer[1] = (byte)(id % 0x100); id = id / 0x100;
            buffer[2] = (byte)(id % 0x100); id = id / 0x100;
            buffer[3] = (byte)(id % 0x100); id = id / 0x100;
            buffer[4] = (byte)cmd;
            return 5;
        }
    }

}
