using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using OpenAX25Core;
using OpenAX25Contracts;
using System.Threading;

namespace OpenAX25_TCPServer
{
    internal class Session
    {
        internal readonly TCPServerChannel m_channel;
        internal readonly Guid m_id;
        internal readonly Socket m_socket;
        internal readonly byte[] m_buffer1;
        internal readonly byte[] m_buffer2;
        internal int m_buffer2size = 0;

        private Timer m_timer;

        internal Session(TCPServerChannel channel, Socket socket)
        {
            m_channel = channel;
            m_socket = socket;
            m_id = Guid.NewGuid();
            m_buffer1 = new byte[channel.m_buffer1alloc];
            m_buffer2 = new byte[channel.m_buffer2alloc];
            m_timer = (channel.m_timerValue > 0)?
                new Timer(OnTimerCallback, this, Timeout.Infinite, Timeout.Infinite):null;
        }

        internal void Open()
        {
            Runtime.Instance.Log(LogLevel.INFO, m_channel.Name, "Open session: " + m_id);
            m_buffer2size = 0;
            TCPServerChannel.ContinueRead(this);
        }

        internal bool Receive(int nReceived)
        {
            Runtime runtime = Runtime.Instance;
            if (runtime.LogLevel >= LogLevel.DEBUG)
            {
                byte[] data = new byte[nReceived];
                runtime.Log(LogLevel.DEBUG, m_channel.Name,
                    "Received on channel " + m_id + ": " + HexConverter.ToHexString(data, true));
            }

            lock (m_buffer2)
            {
                // In text mode look for CR of NL. If found, force output later.
                bool forceOutput = false;
                if (!m_channel.m_binary)
                    for (int i = 0; (i < nReceived) && !forceOutput; ++i)
                        switch (m_buffer1[i]) { case 0x0d : case 0x0a : forceOutput = true; break; default : break; }

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
                        m_buffer2size = 0;
                        iReceived += nToCopy;
                    } // end while //
                }

                // If output was forced and there is data to output, output it now:
                if (forceOutput && (m_buffer2size > 0))
                {
                    Output(m_buffer2, m_buffer2size);
                    m_buffer2size = 0;
                }

                if (startTimer && (m_buffer2size > 0))
                    m_timer.Change(m_channel.m_timerValue, Timeout.Infinite);

                // Allow more input from the channel:
                TCPServerChannel.ContinueRead(this);
            } // end lock //
            return true;
        }

        internal void Close()
        {
            if (m_timer != null)
            {
                m_timer.Change(Timeout.Infinite, Timeout.Infinite);
                m_timer.Dispose();
                m_timer = null;
            }
            if (m_buffer2size > 0)
            {
                Output(m_buffer2, m_buffer2size);
                m_buffer2size = 0;
            }
            Runtime.Instance.Log(LogLevel.INFO, m_channel.Name, "Close session: " + m_id);
        }

        private void Output(byte[] data, int length)
        {
        }

        private static void OnTimerCallback(object obj)
        {
            Session session = (Session)obj;
            if (session.m_timer == null)
                return; // Spurious
            if (session.m_buffer2size > 0)
                lock(session.m_buffer2) {
                    if (session.m_buffer2size > 0)
                    {
                        session.Output(session.m_buffer2, session.m_buffer2size);
                        session.m_buffer2size = 0;
                    }
                }
        }
    }
}
