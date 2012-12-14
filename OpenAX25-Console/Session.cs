using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenAX25Contracts;
using OpenAX25Core;
using System.Collections.Generic;

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
        private bool m_control = false;
        private bool m_server_transmit_binary = false;
        private bool m_client_transmit_binary = false;
        private bool m_client_linemode = false;
        private bool m_client_local_echo = false;

        internal Session(ConsoleChannel channel, Socket socket)
            : base(GetProperties(channel.Properties), null, true)
        {
            m_channel = channel;
            m_socket = socket;
            m_id = ((IPEndPoint)socket.RemoteEndPoint).Port;
            m_buffer1 = new byte[channel.m_buffer1alloc];
            m_buffer2 = new byte[channel.m_buffer2alloc];
        }

        private const byte _WILL = 251;
        private const byte _WOUNT = 252;
        private const byte _DO = 253;
        private const byte _DONT = 254;

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
                    m_target = m_channel.m_localEndpoint.Bind(this, Properties, String.Format("Console({0})", m_id));
                    if (m_target == null)
                        throw new Exception("Unable to bind");
                    m_buffer2size = 0;
                    m_state = SessionState.WAIT_FOR_CONNECT;
                    m_active = true;
                    m_target.Send(new DL_CONNECT_Request());
                    while (m_state == SessionState.WAIT_FOR_CONNECT)
                        Monitor.Wait(m_stateLock);
                    if (m_state != SessionState.CONNECTED)
                        return;
                    ConsoleChannel.ContinueRead(this);

                    byte[] telnetNegotiation = new byte[] {
                        255, _DO,  1, // DO LOCAL_ECHO
                        255, _DO, 34, // DO LINE_MODE
                        255, _WILL,  0, // WILL TRANSMIT BINARY
                        255, _DO,  0, // DO TRANSMIT BINARY
                    };
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Transmit Telnet DO LOCAL_ECHO");
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Transmit Telnet DO LINE_MODE");
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Transmit Telnet WILL TRANSMIT_BINARY");
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Transmit Telnet DO TRANSMIT_BINARY");
                    ConsoleChannel.SendToLocal(this, telnetNegotiation, telnetNegotiation.Length);
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
                    if ((m_target != null) && (m_state != SessionState.DISCONNECTED))
                    {
                        m_target.Send(new DL_DISCONNECT_Request());
                        m_channel.m_localEndpoint.Unbind(m_target);
                        m_target = null;
                    }
                    m_state = SessionState.DISCONNECTED;
                }
                m_channel.Close(this);
            }
            catch (Exception e)
            {
                m_runtime.Log(LogLevel.ERROR,
                    "Console session " + m_id, "Spurious exceptionin \"Close()\": " + e.Message);
            }
        }

        internal bool ReceiveFromLocal(int nReceived)
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
                    int state = 0, i = 0;

                    while (i < nReceived)
                    {
                        byte c = m_buffer1[i++];

                        switch (state)
                        {
                            case 0: // Normal input
                                switch (c)
                                {
                                    case 0x00: // Ignore
                                        break;
                                    case 0x0D: // CR
                                        if ((m_buffer2size + 2) >= m_channel.m_buffer2alloc)
                                        {
                                            --i;
                                        }
                                        else
                                        {
                                            m_buffer2[m_buffer2size++] = 0x0D;
                                            m_buffer2[m_buffer2size++] = 0x0A;
                                        }
                                        goto TX;
                                    case 0x0A: // LF
                                        break;
                                    case 0xFF: // IAC:
                                        state = 1;
                                        break;
                                    default:
                                        if ((m_buffer2size + 1) >= m_channel.m_buffer2alloc)
                                        {
                                            --i;
                                            goto TX;
                                        }
                                        else
                                        {
                                            m_buffer2[m_buffer2size++] = c;
                                            break;
                                        }
                                } // end switch state 0 //
                                break;
                            case 1: // IAC was seen
                                switch (c)
                                {
                                    case 247: // Erase character
                                        if (m_buffer2size > 0)
                                            --m_buffer2size;
                                        state = 0;
                                        break;
                                    case 248: // Erase line
                                        m_buffer2size = 0;
                                        state = 0;
                                        break;
                                    case 249: // Go ahead
                                        state = 0;
                                        goto TX;
                                    case 250: // SB
                                        state = 6;
                                        break;
                                    case 251: // WILL
                                        state = 2;
                                        break;
                                    case 252: // WON'T
                                        state = 3;
                                        break;
                                    case 253: // DO
                                        state = 4;
                                        break;
                                    case 254: // DON'T
                                        state = 5;
                                        break;
                                    case 255: // IAC ESC
                                        if ((m_buffer2size + 1) >= m_channel.m_buffer2alloc)
                                        {
                                            --i;
                                            goto TX;
                                        }
                                        else
                                        {
                                            m_buffer2[m_buffer2size++] = 0xFF;
                                            break;
                                        }
                                    default:
                                        break;
                                } // end switch state 1 //
                                break;
                            case 2: // WILL
                                WILL(c);
                                state = 0;
                                break;
                            case 3: // WOUN'T
                                WOUNT(c);
                                state = 0;
                                break;
                            case 4: // DO
                                DO(c);
                                state = 0;
                                break;
                            case 5: // DON'T
                                DONT(c);
                                state = 0;
                                break;
                            case 6: // Telnet subnegotiation
                                switch (c)
                                {
                                    case 255: // IAC
                                        state = 7;
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case 7: // Telnet subnegotiation IAC
                                switch (c)
                                {
                                    case 240: // SE
                                        state = 0;
                                        break;
                                    default:
                                        state = 6;
                                        break;
                                }
                                break;
                        } // end switch state //
                        continue;
                    TX:
                        if (m_buffer2size > 0)
                        {
                            SendToRemote(m_buffer2, m_buffer2size);
                            m_buffer2size = 0;
                        }
                    } // end while //
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

        private void DONT(byte c) // I am request to don't do something
        {
            switch (c)
            {
                case 0: // Client requests server to don't transmit binary
                    m_runtime.Log(LogLevel.DEBUG, m_name,
                        "Client requests me to don't transmit binary.");
                    if (m_client_transmit_binary)
                    {
                        m_runtime.Log(LogLevel.DEBUG, m_name, "  Accept that.");
                        ConsoleChannel.SendToLocal(this, new byte[] { 255, _WOUNT, c }, 0);
                        m_client_transmit_binary = false;
                    }
                    break;
                default:
                    // WON'T anything else.
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Received Telnet DONT " + (int)c);
                    break;
            } // end switch //
        }

        private void DO(byte c) // I am requested to do something
        {
            switch (c)
            {
                case 0: // Client requests me to transmit binary
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Server shall transmit binary");
                    if (!m_server_transmit_binary)
                    {
                        m_runtime.Log(LogLevel.DEBUG, m_name, "  Acknowledge request.");
                        ConsoleChannel.SendToLocal(this, new byte[] { 255, _WILL, c }, 0);
                        m_server_transmit_binary = true;
                    }
                    break;
                case 1: // Client requests me to local echo.
                    m_runtime.Log(LogLevel.DEBUG, m_name, "  Refuse request.");
                    m_runtime.Log(LogLevel.DEBUG, m_name, "  Deny.");
                    ConsoleChannel.SendToLocal(this, new byte[] { 255, _WOUNT, c }, 3);
                    break;
                default:
                    // DON'T anything else.
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Received Telnet DO " + (int)c);
                    m_runtime.Log(LogLevel.DEBUG, m_name, "  Deny.");
                    ConsoleChannel.SendToLocal(this, new byte[] { 255, _WOUNT, c }, 3);
                    break;
            } // end switch //
        }

        private void WOUNT(byte c) // The peer do not want to do something
        {
            switch (c)
            {
                case 0: // Client don't like to transmit binary.
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Client don't like to transmit binary");
                    if (m_client_transmit_binary)
                    {
                        m_runtime.Log(LogLevel.DEBUG, m_name, "  Acknowledge denial.");
                        ConsoleChannel.SendToLocal(this, new byte[] { 255, _DONT, c }, 3);
                        m_client_transmit_binary = false;
                    }
                    break;
                case 1: // Client don't like to local echo
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Client don't like to local echo");
                    if (m_client_local_echo)
                    {
                        m_runtime.Log(LogLevel.DEBUG, m_name, "  Acknowledge denial.");
                        ConsoleChannel.SendToLocal(this, new byte[] { 255, _DONT, c }, 3);
                        m_client_local_echo = false;
                    }
                    break;
                case 34: // Client don't like to do line mode
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Client don't like to line mode");
                    if (m_client_linemode)
                    {
                        m_runtime.Log(LogLevel.DEBUG, m_name, "  Acknowledge denial.");
                        ConsoleChannel.SendToLocal(this, new byte[] { 255, _DONT, c }, 3);
                    }
                    break;
                default:
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Received Telnet WOUNT " + (int)c);
                    break;
            } // end switch //
        }

        private void WILL(byte c) // The peer do want to do something
        {
            switch (c)
            {
                case 0: // Client agreed to transmit binary
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Client likes to transmit binary");
                    if (!m_client_transmit_binary)
                    {
                        m_runtime.Log(LogLevel.DEBUG, m_name, "  Acknowledge request.");
                        ConsoleChannel.SendToLocal(this, new byte[] { 255, _DO, c }, 3);
                        m_client_transmit_binary = true;
                    }
                    break;
                case 1: // Client agreed to local echo
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Client likes to local echo");
                    if (!m_client_local_echo)
                    {
                        m_runtime.Log(LogLevel.DEBUG, m_name, "  Acknowledge request.");
                        ConsoleChannel.SendToLocal(this, new byte[] { 255, _DO, c }, 3);
                    }
                    break;
                default:
                    // DON'T anything else.
                    m_runtime.Log(LogLevel.DEBUG, m_name, "Received Telnet WILL " + (int)c);
                    m_runtime.Log(LogLevel.DEBUG, m_name, "  Deny.");
                    ConsoleChannel.SendToLocal(this, new byte[] { 255, _DONT, c }, 3);
                    break;
            } // end switch //
        }

        protected override void Input(IPrimitive _p, bool expedited)
        {
            if (!m_active)
                return;
            bool close = false;
            lock (m_stateLock)
            {
                if (!(_p is DataLinkPrimitive))
                    throw new Exception("Expected DataLinkPrimitive. Was: " + _p.GetType().Name);
                DataLinkPrimitive p = (DataLinkPrimitive)_p;
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
                        ReceiveFromRemote(((DL_DATA_Indication)p).Data, false);
                        break;
                    case DataLinkPrimitive_T.DL_UNIT_DATA_Indication_T :
                        ReceiveFromRemote(((DL_UNIT_DATA_Indication)p).Data, true);
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

        private void ReceiveFromRemote(byte[] data, bool control)
        {
            if (data == null)
                return;
            int length = data.Length;
            if (length < 2)
                return;
            int pid = data[0];
            if ((pid != 0x00) && (pid != 0xF0))
            {
                m_runtime.Log(LogLevel.WARNING, m_name, String.Format(
                    "Received unknow PID 0x{0:X2}", pid));
            }

            lock (this)
            {
                if (control != m_control)
                {
                    if (control)
                        ConsoleChannel.SendToLocal(this, new byte[] { 0x0D, 0x0A, 0x07 }, 3);
                    else
                        ConsoleChannel.SendToLocal(this, new byte[] { 0x0D, 0x0A }, 2);
                    m_control = control;
                }

                int i = 1, j;
                int a = (int)(1.5 * (float)length);
                byte[] b = new byte[a];
                while (i < length)
                {
                    j = 0;
                    while ((i < length) && (j < a))
                    {
                        byte x = data[i++];
                        switch (x)
                        {
                            case 0x0D: // Allways use CR LF sequence.
                                if (j + 2 >= a)
                                {
                                    --i;
                                }
                                else
                                {
                                    b[j++] = 0x0D;
                                    b[j++] = 0x0A;
                                }
                                goto L0;
                            case 0x0A:
                                break;
                            case 0x00:
                                break;
                            case 0xFF: // Escape IAC
                                if (j + 2 >= a)
                                {
                                    --i;
                                    goto L0;
                                }
                                b[j++] = 0xFF;
                                b[j++] = 0xFF;
                                break;
                            default:
                                b[j++] = x;
                                break;
                        } // end switch //
                    } // end while //
                L0:
                    if (j >= 0)
                        ConsoleChannel.SendToLocal(this, b, j + 1);
                    if (i >= length)
                        break;
                    a = (int)(1.5 * (float)(length - i));
                    b = new byte[a];
                } // end while //
            } // end lock //
        }

        private void SendToRemote(byte[] data, int length)
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
                        byte[] frame = new byte[length];
                        frame[0] = 0xF0; //  No L3 protocol.
                        Array.Copy(data, 1, frame, 1, length - 1);
                        m_target.Send(new DL_UNIT_DATA_Request(frame));
                    }
                    else
                    {
                        byte[] frame = new byte[length+1];
                        frame[0] = 0xF0; //  No L3 protocol.
                        Array.Copy(data, 0, frame, 1, length);
                        m_target.Send(new DL_DATA_Request(frame));
                    }
                }
            }
        }

        private static IDictionary<string, string> GetProperties(IDictionary<string, string> properties)
        {
            IDictionary<string, string> _properties = (properties == null) ?
                new Dictionary<string, string>() :
                new Dictionary<string, string>(properties);
            _properties.Remove("Target");
            return _properties;
        }

    }

}
