using System;
using System.Collections.Generic;
using System.Threading;
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25_Protocol
{

    internal delegate void OnDataLinkOutputEventHandler(DataLinkPrimitive p);
    internal delegate void OnLinkMultiplexerOutputEventHandler(LinkMultiplexerPrimitive p);
    internal delegate void OnAX25OutputEventHandler(AX25Frame f);

    internal class DataLinkStateMachine
    {

        /// <summary>
        /// Send State Variable V(S).
        /// The send state variable exists within the TNC and is never sent. It contains the next sequential number to be
        /// assigned to the next transmitted I frame. This variable is updated with the transmission of each I frame.
        /// </summary>
        private int V_S
        {
            get
            {
                return _v_s;
            }
            set
            {
                if (value == _v_s)
                    return;
                _v_s = value % (int)m_modulo;
                lock (IFrameQueue)
                {
                    Monitor.PulseAll(IFrameQueue);
                }
            }
        }
        private int _v_s = 0;

        /// <summary>
        /// Acknowledge State Variable V(A).
        /// The acknowledge state variable exists within the TNC and is never sent. It contains the sequence number of
        /// the last frame acknowledged by its peer [V(A)-1 equels the N(S) of the last acknowledged I frame].
        /// </summary>
        private int V_A
        {
            get
            {
                return _v_a;
            }
            set
            {
                if (value == _v_a)
                    return;
                _v_a = value % (int)m_modulo;
                lock (IFrameQueue)
                {
                    Monitor.PulseAll(IFrameQueue);
                }
            }
        }
        private int _v_a = 0;

        /// <summary>
        /// Receive State Variable V(R).
        /// The receive state variable exists within the TNC. It contains the sequence number of the next expected
        /// received I frame. This variable is updated upon the reception of an error-free I frame whose send sequence
        /// number equals the present received state variable value.
        /// </summary>
        private int V_R
        {
            get
            {
                return _v_r;
            }
            set
            {
                _v_r = value % (int)m_modulo;
            }
        }
        private int _v_r = 0;

        /// <summary>
        /// Send Sequence Number N(S).
        /// The send sequence number is found in the control field of all I frames. It contains the sequence number of the
        /// I frame being sent. Just prior to the transmission of the I frame, N(S) is updated to equel the send state variable.
        /// </summary>
        private int N_S = 0;

        /// <summary>
        /// Received Sequence Number N(R).
        /// The received sequence number exists in both I and S frames. Prior to sending an I or S frame, this variable is
        /// updated to equal that of the received state variable, thus implicitly acknowledging the proper reception of all
        /// frames up to and including N(R)-1;
        /// </summary>
        private int N_R = 0;

        /// <summary>
        /// Window Size Receive.
        /// </summary>
        private int k = 4;

        /// <summary>
        /// Buffer of information to be transmitted in I frames.
        /// </summary>
        private byte[][] IFrameQueue = new byte[128][];
        private int LFrameQueue
        {
            get
            {
                int l = V_S - V_A;
                return (l >= 0) ? l : l + (int)m_modulo;
            }
        }

        internal enum State_T
        {
            Disconnected = 0,
            AwaitingConnect = 1,
            AwaitingRelease = 2,
            Connected = 3,
            TimerRecovery = 4,
            AwaitingConnect2_2 = 5
        }

        private readonly static IDictionary<State_T, string> State_N = new Dictionary<State_T, string> {
            { State_T.Disconnected,       "Disconnected"         },
            { State_T.AwaitingConnect,    "Awaiting Connect"     },
            { State_T.AwaitingRelease,    "Awaiting Release"     },
            { State_T.Connected,          "Connected"            },
            { State_T.TimerRecovery,      "Timer Recovery"       },
            { State_T.AwaitingConnect2_2, "Awaiting Connect V2.2"},
        };

        private State_T m_state = State_T.Disconnected;

        internal static string GetStateName(State_T state)
        {
            return State_N[state];
        }

        internal State_T State {
            get {
                return m_state;
            }
            set
            {
                if (value == m_state)
                    return;
                m_state = value;
                lock (IFrameQueue)
                {
                    Monitor.PulseAll(IFrameQueue);
                }
            }
        }

        internal string StateName {
            get {
                return State_N[m_state];
            }
        }

        internal enum ErrorCode_T
        {
            ErrorA = 'A', ErrorB = 'B', ErrorC = 'C', ErrorD = 'D', ErrorE = 'E', ErrorF = 'F',
            ErrorG = 'G', ErrorH = 'H', ErrorI = 'I', ErrorJ = 'J', ErrorK = 'K', ErrorL = 'L', 
            ErrorM = 'M', ErrorN = 'N', ErrorO = 'O', ErrorP = 'P', ErrorQ = 'Q', ErrorR = 'R',
            ErrorS = 'S', ErrorT = 'T', ErrorU = 'U', ErrorV = 'V'
        }

        private readonly static IDictionary<ErrorCode_T, string> ErrorCode_N = new Dictionary<ErrorCode_T, string>
        {
            { ErrorCode_T.ErrorA, "F=1 received but P=1 not outstanding" },
            { ErrorCode_T.ErrorB, "Unexpected DM with F=1 in states 3, 4 or 5" },
            { ErrorCode_T.ErrorC, "Unexcepcted UA in states 3, 4 or 5" },
            { ErrorCode_T.ErrorD, "UA received without F=1 when SABM or DISC was sent P=1" },
            { ErrorCode_T.ErrorE, "DM received in states 3, 4 or 5" },
            { ErrorCode_T.ErrorF, "Data link reset; i.e., SABM received in state 3, 4 or 5" },
            { ErrorCode_T.ErrorG, "Too many retries" },
            { ErrorCode_T.ErrorH, "Too many retries" },
            { ErrorCode_T.ErrorI, "m_config.Initial_N2 timouts; unacknowledged data" },
            { ErrorCode_T.ErrorJ, "N(r) sequence error" },
            { ErrorCode_T.ErrorK, "Frame reject in connected state" },
            { ErrorCode_T.ErrorL, "Control field invalid or not implemented" },
            { ErrorCode_T.ErrorM, "Information field was receiced in a U- or S-type frame" },
            { ErrorCode_T.ErrorN, "Length of frame incorrect for frame type" },
            { ErrorCode_T.ErrorO, "I frame exceeded maximum allowed length" },
            { ErrorCode_T.ErrorP, "N(s) out of the window" },
            { ErrorCode_T.ErrorQ, "UI response received, or UI command with P=1 received" },
            { ErrorCode_T.ErrorR, "UI frame exceeded maximum allowed length" },
            { ErrorCode_T.ErrorS, "I response received" },
            { ErrorCode_T.ErrorT, "N2 timeouts: no response to enquiry" },
            { ErrorCode_T.ErrorU, "N2 timeouts: extended peer busy condition" },
            { ErrorCode_T.ErrorV, "No DL machines available to establish connection" }
        };

        internal static string ErrorCodeName(ErrorCode_T errorCode)
        {
            return ErrorCode_N[errorCode];
        }

        internal event OnDataLinkOutputEventHandler OnDataLinkOutputEvent;
        internal event OnLinkMultiplexerOutputEventHandler OnLinkMultiplexerOutputEvent;
        internal event OnAX25OutputEventHandler OnAX25OutputEvent;
        internal event OnAX25OutputEventHandler OnAX25OutputExpeditedEvent;
        internal AX25Modulo m_modulo = AX25Modulo.MOD8;

        private bool Layer3Initiated = false; // SABM was sent by request of Layer 3; i.e., DL-CONNECT primitive.
        private bool PeerReceiverBusy = false; // Remote station is busy and cannot receive I frames.
        private bool OwnReceiverBusy  = false; // Layer 3 is busy and cannot receive I frames.
        private bool RejectException = false; // A REJ frame has been sent to the remote station.
        private bool SelectiveRejectException = false; // A SREJ frame has been sent to the remote station.
        private bool AcknowledgePending = false; // I frames have been suddenly received but not yet acknowledged to the remote station.
        private long SAT = 100;
        private long SRT = 100; // Smoothed round trip time.
        private long T1V = 100; // Next value for T1; default initial value is initial value of SRT
        private long T3V = 10000;
        private int N1R = 2048;
        private int T2 = 3000;
        private int RC = 0;
        private int SRejectException;
        private bool F = false; // Final bit
        private bool P = false; // Poll bit
        private bool SREJEnabled;
        private readonly Runtime m_runtime = Runtime.Instance;
        private readonly AX25Timer T1; // Outstanding I frame or P-bit.
        private readonly AX25Timer T3; // Idle supervision (keep alive).
        private readonly LinkMultiplexerStateMachine m_multiplexer;
        private readonly AX25_I[] IFrameStore = new AX25_I[128];

        private static void OnT1Callback(object obj)
        {
            ((DataLinkStateMachine)obj).TimerT1Expiry();
        }

        private static void OnT3Callback(object obj)
        {
            ((DataLinkStateMachine)obj).TimerT3Expiry();
        }

        private AX25_Configuration m_config;
        private AX25Version m_version
        {
            get
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD128:
                        return AX25Version.V2_2;
                    default :
                        return AX25Version.V2_0;
                }
            }
            set
            {
                switch (value)
                {
                    case AX25Version.V2_0:
                        m_modulo = AX25Modulo.MOD8;
                        break;
                    case AX25Version.V2_2:
                        m_modulo = AX25Modulo.MOD128;
                        break;
                }
            }
        }

        internal DataLinkStateMachine(AX25_Configuration config)
        {
            m_config = config;
            m_version = config.Initial_version;
            m_multiplexer = new LinkMultiplexerStateMachine();
            T1 = new AX25Timer(this, new TimerCallback(OnT1Callback));
            T3 = new AX25Timer(this, new TimerCallback(OnT3Callback));
        }

        internal void Input(DataLinkPrimitive p)
        {
        RETRY:
            if (p.DataLinkPrimitiveType == DataLinkPrimitive_T.DL_DATA_Request_T)
                lock (IFrameQueue)
                {
                    while ((State != State_T.Disconnected) && (LFrameQueue >= k))
                        Monitor.Wait(IFrameQueue);
                }
            lock (this)
            {
                if ((p.DataLinkPrimitiveType == DataLinkPrimitive_T.DL_DATA_Request_T) &&
                    ((State == State_T.Disconnected) || (LFrameQueue >= k)))
                    goto RETRY;
                switch (State)
                {
                    case State_T.Disconnected: Disconnected(p); break;
                    case State_T.Connected: Connected(p); break;
                    case State_T.AwaitingConnect: AwaitingConnect(p); break;
                    case State_T.AwaitingRelease: AwaitingRelease(p); break;
                    case State_T.TimerRecovery: TimerRecovery(p); break;
                    case State_T.AwaitingConnect2_2: AwaitingConnect2_2(p); break;
                } // end switch //
            }
        }

        internal void Input(LinkMultiplexerPrimitive p)
        {
            lock (this)
            {
                switch (State)
                {
                    case State_T.Connected: Connected(p); break;
                    case State_T.TimerRecovery: TimerRecovery(p); break;
                    default: break;
                } // end switch //
            }
        }

        internal void Input(AX25Frame f)
        {
            lock (this)
            {
                if (f is AX25_I)
                {
                    AX25_I i = (AX25_I)f;
                    P = i.P;
                    F = false;
                    N_R = i.N_R;
                    N_S = i.N_S;
                }
                else if (f is AX25SFrame)
                {
                    AX25SFrame s = (AX25SFrame)f;
                    P = (f.Command && s.PF);
                    F = (f.Response && s.PF);
                    N_R = s.N_R;
                    N_S = -1;
                }
                else if (f is AX25UFrame)
                {
                    AX25UFrame u = (AX25UFrame)f;
                    P = (f.Command && u.PF);
                    F = (f.Response && u.PF);
                    N_R = -1;
                    N_S = -1;
                }
                TraceLog("%INP[" + f.ToString() + "]");
                TraceLog(
                    "::::: V_A=" + V_A + ",N_R=" + N_R + ",V_S=" + V_S + ",V_R=" + V_R + ",N_S=" + N_S);
                switch (State)
                {
                    case State_T.Disconnected: Disconnected(f); break;
                    case State_T.Connected: Connected(f); break;
                    case State_T.AwaitingConnect: AwaitingConnect(f); break;
                    case State_T.AwaitingRelease: AwaitingRelease(f); break;
                    case State_T.TimerRecovery: TimerRecovery(f); break;
                    case State_T.AwaitingConnect2_2: AwaitingConnect2_2(f); break;
                } // end switch //
            } // end lock (this) //
        }

        private void Disconnected(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T :
                    goto L0060;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T :
                    goto L0080;
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    goto L0120;
                default :
                    goto L0100;
            } // end switch //

        L0060:
            OnDataLinkOutputEvent(new DL_DISCONNECT_Confirm());
            State = State_T.Disconnected;
            return;

        L0080:
            UI_Command(((DL_UNIT_DATA_Request)p).Data);
            State = State_T.Disconnected;
            return;

        L0100:
            State = State_T.Disconnected;
            return;

        L0120:
            this.SAT = m_config.Initial_SAT;
            this.T1V = 2 * this.SAT;
            EstablishDataLink(((DL_CONNECT_Request)p).Version);
            Layer3Initiated = true;
            State = State_T.AwaitingConnect;
            return;
        }

        private void Disconnected(AX25Frame f)
        {
            switch (f.FrameType)
            {
                case AX25Frame_T.UA:
                    goto L0030;
                case AX25Frame_T.DM:
                    goto L0040;
                case AX25Frame_T.UI:
                    goto L0050;
                case AX25Frame_T.DISC:
                    goto L0060;
                case AX25Frame_T.SABM:
                    goto L0130;
                case AX25Frame_T.SABME:
                    goto L0150;
                default:
                    if (f.Command)
                        goto L0090;
                    goto L0010;
            } // end switch //

        L0010:
            State = State_T.Disconnected;
            return;

        L0030:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorC));
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorD));
            State = State_T.Disconnected;
            return;

        L0040:
            State = State_T.Disconnected;
            return;

        L0050:
            UI_Check((AX25_UI)f);
            if (!P)
                goto L0051;
            OnAX25OutputEvent(new AX25_DM(true, false, true));

        L0051:
            State = State_T.Disconnected;
            return;

        L0060:
            F = P;
            OnAX25OutputEvent(new AX25_DM(F, false, true));
            State = State_T.Disconnected;
            return;

        L0090:
            F = P;
            OnAX25OutputEvent(new AX25_DM(F, false, true));
            State = State_T.Disconnected;
            return;

        L0130:
            F = P;
            if (AbleToEstablish())
            {
                SetVersion(AX25Version.V2_0);
                goto L0141;
            }
            OnAX25OutputEvent(new AX25_DM(F, false, true));
            State = State_T.Disconnected;
            return;

        L0141:
            OnAX25OutputEvent(new AX25_UA(F, false, true));
            ClearExceptionConditions();
            V_S = 0;
            V_A = 0;
            V_R = 0;
            OnDataLinkOutputEvent(new DL_CONNECT_Indication("<TODO>", m_version));
            SRT = m_config.Initial_SRT;
            T1V = 2 * SRT;
            T3.Start(T3V);
            State = State_T.Connected;
            return;

        L0150:
            F = P;
            if (AbleToEstablish())
            {
                SetVersion(AX25Version.V2_2);
                goto L0141;
            }
            OnAX25OutputEvent(new AX25_DM(F, false, true));
            State = State_T.Disconnected;
            return;
        }

        private void AwaitingConnect(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    goto L1030;
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T:
                    goto L1040;
                case DataLinkPrimitive_T.DL_DATA_Request_T:
                    goto L1070;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T:
                    goto L1100;
                default:
                    goto L1110;
            } // end switch //

        L1030:
            IFrameQueue_Clear();
            Layer3Initiated = true;
            State = State_T.AwaitingConnect;
            return;

        L1040:
            IFrameQueue_Clear();
            RC = 0;
            P = true;
            OnAX25OutputEvent(new AX25_DM(P, true, false));
            T3.Stop();
            T1.Start(T1V);
            State = State_T.AwaitingRelease;
            return;

        L1070:
            if (Layer3Initiated)
                goto L1071;
            IFrameQueue[N_S++] = ((DL_DATA_Request)p).Data;

        L1071:
            State = State_T.AwaitingConnect;
            return;

        L1100:
            UI_Command(((DL_UNIT_DATA_Request)p).Data);
            State = State_T.AwaitingConnect;
            return;

        L1110:
            State = State_T.AwaitingConnect;
            return;
        }

        private void AwaitingConnect(AX25Frame f)
        {
            switch (f.FrameType)
            {
                case AX25Frame_T.SABM:
                    goto L1050;
                case AX25Frame_T.DISC:
                    goto L1060;
                case AX25Frame_T.UI:
                    goto L1090;
                case AX25Frame_T.DM:
                    goto L1130;
                case AX25Frame_T.UA:
                    goto L1150;
                case AX25Frame_T.SABME:
                    goto L1190;
                default:
                    goto L1120;
            } // end switch //

        L1050:
            F = P;
            OnAX25OutputEvent(new AX25_UA(F, false, true));
            State = State_T.AwaitingConnect;
            return;

        L1060:
            F = P;
            OnAX25OutputEvent(new AX25_DM(F, false, true));
            State = State_T.AwaitingConnect;
            return;

        L1090:
            UI_Check((AX25_UI)f);
            if (!P)
                goto L1091;
            F = P;
            OnAX25OutputEvent(new AX25_DM(F, false, true));

        L1091:
            State = State_T.AwaitingConnect;
            return;

        L1120:
            State = State_T.AwaitingConnect;
            return;

        L1130:
            if (!F)
            {
                State = State_T.AwaitingConnect;
                return;
            }
            IFrameQueue_Clear();
            OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
            T1.Stop();
            State = State_T.Disconnected;
            return;

        L1141:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorD));
            State = State_T.AwaitingConnect;
            return;

        L1150:
            if (!F)
                goto L1141;
            if (!Layer3Initiated)
                goto L1161;
            OnDataLinkOutputEvent(new DL_CONNECT_Confirm());

        L1151:
            T1.Stop();
            T3.Stop();
            V_S = 0;
            V_A = 0;
            V_R = 0;
            SelectT1Value();
            State = State_T.Connected;
            return;

        L1161:
            if (V_S == V_A)
                goto L1151;
            IFrameQueue_Clear();
            OnDataLinkOutputEvent(new DL_CONNECT_Indication("<TODO>", AX25Version.V2_0));
            goto L1151;

        L1190:
            F = P;
            OnAX25OutputEvent(new AX25_DM(F, false, true));
            State = State_T.AwaitingConnect2_2;
            return;

        }

        private void AwaitingConnect2_2(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T:
                    goto L5030;
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    goto L5080;
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T:
                    goto L5090;
                case DataLinkPrimitive_T.DL_DATA_Request_T:
                    goto L5120;
                default:
                    goto L5110;
            } // end switch //

        L5030:
            UI_Command(((DL_UNIT_DATA_Request)p).Data);
            State = State_T.AwaitingConnect2_2;
            return;

        L5080:
            IFrameQueue_Clear();
            Layer3Initiated = true;
            State = State_T.AwaitingConnect2_2;
            return;

        L5090:
            IFrameQueue_Clear();
            RC = 0;
            P = true;
            OnAX25OutputEvent(new AX25_DM(P, true, false));
            T3.Stop();
            T1.Start(T1V);
            State = State_T.AwaitingRelease;
            return;

        L5120:
            if (Layer3Initiated)
                goto L5121;
            IFrameQueue[N_S++] = ((DL_DATA_Request)p).Data;

        L5121:
            State = State_T.AwaitingConnect2_2;
            return;

        L5110:
            State = State_T.AwaitingConnect2_2;
            return;
        }

        private void AwaitingConnect2_2(AX25Frame f)
        {
            switch (f.FrameType)
            {
                case AX25Frame_T.UI:
                    goto L5020;
                case AX25Frame_T.SABM:
                    goto L5100;
                case AX25Frame_T.DISC:
                    goto L5110;
                case AX25Frame_T.DM:
                    goto L5140;
                case AX25Frame_T.UA:
                    goto L5170;
                case AX25Frame_T.SABME:
                    goto L5210;
                case AX25Frame_T.FRMR:
                    goto L5220;
                default:
                    goto L5040;
            } // end switch //

        L5020:
            UI_Check((AX25_UI)f);
            if (!P)
                goto L5021;
            F = P;
            OnAX25OutputEvent(new AX25_DM(F, false, true));

        L5021:
            State = State_T.AwaitingConnect2_2;
            return;

        L5040:
            State = State_T.AwaitingConnect2_2;
            return;

        L5100:
            F = P;
            OnAX25OutputEvent(new AX25_UA(F, false, true));
            State = State_T.AwaitingConnect;
            return;

        L5110:
            F = P;
            OnAX25OutputEvent(new AX25_DM(F, false, true));
            State = State_T.AwaitingConnect2_2;
            return;

        L5140:
            if (!F)
                goto L5150;
            IFrameQueue_Clear();
            OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
            T1.Stop();
            State = State_T.Disconnected;
            return;

        L5150:
            State = State_T.AwaitingConnect2_2;
            return;

        L5161:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorD));
            State = State_T.AwaitingConnect2_2;
            return;

        L5170:
            if (!F)
                goto L5161;
            if (!Layer3Initiated)
                goto L5181;
            OnDataLinkOutputEvent(new DL_CONNECT_Confirm());

        L5171:
            T1.Stop();
            T3.Stop();
            V_S = 0;
            V_A = 0;
            V_R = 0;
            SelectT1Value();
            State = State_T.Connected;
            return;

        L5181:
            if (V_S == V_A)
                goto L5171;
            IFrameQueue_Clear();
            OnDataLinkOutputEvent(new DL_CONNECT_Indication("<TODO>", m_version));
            goto L5171;

        L5210:
            F = P;
            OnAX25OutputEvent(new AX25_UA(F, false, true));
            State = State_T.AwaitingConnect2_2;
            return;

        L5220:
            SRT = m_config.Initial_SRT;
            T1V = 2 * SRT;
            EstablishDataLink(m_version);
            Layer3Initiated = true;
            State = State_T.AwaitingConnect;
            return;
        }

        private void AwaitingRelease(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T:
                    goto L2030;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T:
                    goto L2060;
                default:
                    goto L2090;
            } // end switch //

        L2030:
            OnAX25OutputExpeditedEvent(new AX25_DM(true));
            T1.Stop();
            State = State_T.Disconnected;
            return;

        L2060:
            UI_Command(((DL_UNIT_DATA_Request)p).Data);
            State = State_T.AwaitingRelease;
            return;

        L2090:
            State = State_T.Disconnected;
            return;
        }

        private void AwaitingRelease(AX25Frame f)
        {
            switch (f.FrameType)
            {
                case AX25Frame_T.SABM:
                    goto L2040;
                case AX25Frame_T.DISC:
                    goto L2050;
                case AX25Frame_T.I:
                case AX25Frame_T.RR:
                case AX25Frame_T.RNR:
                case AX25Frame_T.REJ:
                case AX25Frame_T.SREJ:
                    if (f.Command)
                        goto L2070;
                    else
                        goto L2100;
                case AX25Frame_T.UI:
                    goto L2080;
                case AX25Frame_T.UA:
                    goto L2110;
                case AX25Frame_T.DM:
                    goto L2140;
                default:
                    goto L2100;
            } // end switch //

        L2040:
            F = P;
            OnAX25OutputExpeditedEvent(new AX25_DM(F, false, true));
            State = State_T.AwaitingRelease;
            return;

        L2050:
            F = P;
            OnAX25OutputExpeditedEvent(new AX25_UA(F, false, true));
            State = State_T.AwaitingRelease;
            return;

        L2070:
            if (!P)
                goto L2071;
            F = true;
            OnAX25OutputEvent(new AX25_DM(F, false, true));

        L2071:
            State = State_T.AwaitingRelease;
            return;

        L2080:
            if (!P)
                goto L2081;
            UI_Check((AX25_UI)f);
            F = true;
            OnAX25OutputEvent(new AX25_DM(F, false, true));

        L2081:
            State = State_T.AwaitingRelease;
            return;

        L2100:
            State = State_T.AwaitingRelease;
            return;

        L2110:
            if (!F)
                goto L2121;
            OnDataLinkOutputEvent(new DL_DISCONNECT_Confirm());
            T1.Stop();
            State = State_T.Disconnected;
            return;

        L2121:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorD));

        L2131:
            State = State_T.AwaitingRelease;
            return;

        L2140:
            if (!F)
                goto L2131;
            OnDataLinkOutputEvent(new DL_CONNECT_Confirm());
            T1.Stop();
            State = State_T.Disconnected;
            return;
        }

        private void Connected(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    goto L3030;
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T:
                    goto L3040;
                case DataLinkPrimitive_T.DL_DATA_Request_T:
                    goto L3050;
                case DataLinkPrimitive_T.DL_FLOW_OFF_Request_T:
                    goto L3160;
                case DataLinkPrimitive_T.DL_FLOW_ON_Request_T:
                    goto L3170;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T:
                    goto L3180;
                default:
                    return;
            } // end switch //

        L3030:
            IFrameQueue_Clear();
            EstablishDataLink(m_version);
            Layer3Initiated = true;
            State = State_T.AwaitingConnect;
            return;

        L3040:
            IFrameQueue_Clear();
            RC = 0;
            OnAX25OutputEvent(new AX25_DISC(true));
            T3.Stop();
            T1.Start(T1V);
            State = State_T.AwaitingRelease;
            return;

        L3050:
            IFrameQueue[N_S++] = ((DL_DATA_Request)p).Data;
            State = State_T.Connected;
            return;

        L3160:
            if (OwnReceiverBusy)
                goto L3161;
            OwnReceiverBusy = true;
            F = false;
            OnAX25OutputEvent(new AX25_RNR(m_modulo, N_R, F, false, true));
            AcknowledgePending = false;

        L3161:
            State = State_T.Connected;
            return;

        L3170:
            if (!OwnReceiverBusy)
                goto L3161;
            OwnReceiverBusy = false;
            P = true;
            OnAX25OutputEvent(new AX25_RR(m_modulo, N_R, P, true, false));
            AcknowledgePending = false;
            if (T1.Running)
                goto L3161;
            T3.Stop();
            T1.Start(T1V);
            goto L3161;

        L3180:
            UI_Command(((DL_UNIT_DATA_Request)p).Data);
            State = State_T.Connected;
            return;
        }

        private void Connected(LinkMultiplexerPrimitive p)
        {
            switch (p.LinkMultiplexerPrimitiveType)
            {
                case LinkMultiplexerPrimitive_T.LM_SEIZE_Confirm_T:
                    goto L3220;
                default:
                    return;
            } // end switch //

        L3220:
            if (!AcknowledgePending)
                goto L3221;
            AcknowledgePending = false;
            EnquiryResponse(false);

        L3221:
            OnLinkMultiplexerOutputEvent(new LM_RELEASE_Request(m_multiplexer));
            State = State_T.Connected;
            return;
        }

        private void Connected(AX25Frame f)
        {
            switch (f.FrameType)
            {
                case AX25Frame_T.SABM:
                    goto L3100;
                case AX25Frame_T.SABME:
                    goto L3110;
                case AX25Frame_T.DISC:
                    goto L3120;
                case AX25Frame_T.UA:
                    goto L3130;
                case AX25Frame_T.DM:
                    goto L3140;
                case AX25Frame_T.FRMR:
                    goto L3150;
                case AX25Frame_T.UI:
                    goto L3190;
                case AX25Frame_T.RR:
                    goto L3200;
                case AX25Frame_T.RNR:
                    goto L3210;
                case AX25Frame_T.SREJ:
                    goto L3230;
                case AX25Frame_T.REJ:
                    goto L3250;
                case AX25Frame_T.I:
                    goto L3270;
                default:
                    return;
            } // end switch //

        L3100:

        L3101:
            F = P;
            OnAX25OutputEvent(new AX25_UA(F, false, true));
            ClearExceptionConditions();
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorF));
            if (V_S == V_A)
                goto L3102;
            IFrameQueue_Clear();
            OnDataLinkOutputEvent(new DL_CONNECT_Indication("<TODO>", m_version));

        L3102:
            T1.Stop();
            T3.Start(T3V);
            V_S = 0;
            V_A = 0;
            V_R = 0;
            State = State_T.Connected;
            return;

        L3110:
            goto L3101;

        L3120:
            IFrameQueue_Clear();
            F = P;
            OnAX25OutputEvent(new AX25_UA(F, false, true));
            OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
            T1.Stop();
            T3.Stop();
            State = State_T.Disconnected;
            return;

        L3130:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorC));
            EstablishDataLink(m_version);
            Layer3Initiated = false;
            State = State_T.AwaitingConnect;
            return;

        L3140:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorE));
            OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
            IFrameQueue_Clear();
            T1.Stop();
            T3.Stop();
            State = State_T.Disconnected;
            return;

        L3150:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorK));
            EstablishDataLink(m_version);
            Layer3Initiated = false;
            State = State_T.AwaitingConnect;
            return;

        L3190:
            UI_Check((AX25_UI)f);
            if (!P)
                goto L3191;
            EnquiryResponse(true);

        L3191:
            State = State_T.Connected;
            return;

        L3200:
            PeerReceiverBusy = false;

        L3201:
            CheckNeedForResponse(f);
            if (!(LE(V_A, N_R) && LE(N_R, V_S)))
                goto L3211;
            CheckIFrameAcknowledged();
            State = State_T.Connected;
            return;

        L3210:
            PeerReceiverBusy = true;
            goto L3201;

        L3211:
            NrErrorRecovery();
            State = State_T.AwaitingConnect;
            return;

        L3230:
            PeerReceiverBusy = false;
            CheckNeedForResponse(f);
            if (!(LE(V_A, N_R) && LE(N_R, V_S)))
                goto L3241;
            if (!P)
                goto L3231;
            V_A = N_R;

        L3231:
            T1.Stop();
            T3.Start(T3V);
            SelectT1Value();
            P = true;
            OnAX25OutputEvent(new AX25_I(IFrameQueue[N_R], m_modulo, N_R, N_S, P, true, false));
            State = State_T.Connected;
            return;

        L3241:
            NrErrorRecovery();
            State = State_T.AwaitingConnect;
            return;

        L3250:
            PeerReceiverBusy = false;
            CheckNeedForResponse(f);
            if (!(LE(V_A, N_R) && LE(N_R, V_S)))
                goto L3261;
            V_A = N_R;
            T1.Stop();
            T3.Stop();
            SelectT1Value();
            InvokeRetransmission();
            State = State_T.Connected;
            return;

        L3261:
            NrErrorRecovery();
            State = State_T.AwaitingConnect;
            return;

        L3270:
            TraceLog("***** L3270");
            if (!((AX25_I)f).Command)
                goto L3291;
            if (((AX25_I)f).InfoFieldLength > m_config.Initial_N1)
                goto L3281;
            if (!(LE(V_A, N_R) && LE(N_R, V_S)))
                goto L3261;
            CheckIFrameAcknowledged();
            goto L3300;

        L3281:
            TraceLog("***** L3281");
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorO));
            EstablishDataLink(m_version);
            Layer3Initiated = false;
            State = State_T.AwaitingConnect;
            return;

        L3291:
            TraceLog("***** L3291");
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorS));
            /*DiscardIframe()*/
            State = State_T.Connected;
            return;

        L3300:
            TraceLog("***** L3300");
            if (OwnReceiverBusy)
                goto L3361;
            if (N_S != V_R)
                goto L3321;
            V_R++;
            RejectException = false;
            if (SRejectException > 0)
                --SRejectException;
            OnDataLinkOutputEvent(new DL_DATA_Indication(((AX25_I)f).I));

        L3301:
            TraceLog("***** L3301");
            if (IFrameStore[V_R] == null)
                goto L3311;
            f = IFrameStore[V_R];
            N_R = ((AX25_I)f).N_R;
            N_S = ((AX25_I)f).N_S;
            P = ((AX25_I)f).P;
            OnDataLinkOutputEvent(new DL_DATA_Indication(((AX25_I)f).I));
            V_R++;
            goto L3301;

        L3311:
            TraceLog("***** L3311");
            if (P)
                goto L3322;
            if (AcknowledgePending)
                goto L3323;
            OnLinkMultiplexerOutputEvent(new LM_SEIZE_Request(m_multiplexer));
            AcknowledgePending = true;
            goto L3323;

        L3321:
            TraceLog("***** L3321");
            if (!RejectException)
                goto L3331;
            /*DiscardContentsOfIFrame();*/
            if (!P)
                goto L3323;

        L3322:
            TraceLog("***** L3322");
            F = true;
            N_R = V_R;
            OnAX25OutputEvent(new AX25_RR(m_modulo, N_R, F, false, true));
            AcknowledgePending = false;

        L3323:
            TraceLog("***** L3323");
            State = State_T.Connected;
            return;

        L3331:
            TraceLog("***** L3331");
            if (SREJEnabled)
                goto L3341;

        L3332:
            TraceLog("***** L3332");
            /*DiscardContentsOfIFrame();*/
            RejectException = true;
            F = P;
            N_R = V_R;
            OnAX25OutputEvent(new AX25_REJ(m_modulo, N_R, F, false, true));

        L3333:
            TraceLog("***** L3333");
            AcknowledgePending = false;
            goto L3323;

        L3341:
            TraceLog("***** L3341");
            IFrameStore[N_R] = (AX25_I)f;
            if (SRejectException > 0)
                goto L3351;
            if (GT(N_S, V_R + 1))
                goto L3332;
            N_R = V_R;
            F = true;

        L3342:
            TraceLog("***** L342");
            ++SRejectException;
            OnAX25OutputEvent(new AX25_SREJ(m_modulo, N_R, F, false, true));
            goto L3333;

        L3351:
            TraceLog("***** L3351");
            N_R = N_S;
            F = false;
            goto L3342;

        L3361:
            TraceLog("***** L3361");
            /*DiscardContentsOfIFrame();*/
            if (!P)
                goto L3362;
            F = true;
            OnAX25OutputEvent(new AX25_RNR(m_modulo, N_R, F, false, true));
            AcknowledgePending = false;

        L3362:
            TraceLog("***** L3362");
            State = State_T.Connected;
            return;

        }

        private void TimerRecovery(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType) {
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    goto L4030;
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T:
                    goto L4040;
                case DataLinkPrimitive_T.DL_DATA_Request_T:
                    goto L4050;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T:
                    goto L4230;
                case DataLinkPrimitive_T.DL_FLOW_OFF_Request_T:
                    goto L4280;
                case DataLinkPrimitive_T.DL_FLOW_ON_Request_T:
                    goto L4290;
                default:
                    return;
            } // end switch //

        L4030:
            IFrameQueue_Clear();
            EstablishDataLink(m_version);
            Layer3Initiated = true;
            State = State_T.AwaitingConnect;
            return;

        L4040:
            IFrameQueue_Clear();
            RC = 0;
            P = true;
            OnAX25OutputEvent(new AX25_DISC(P, true, false));
            T3.Stop();
            T1.Start(T1V);
            State = State_T.AwaitingRelease;
            return;

        L4050:
            IFrameQueue[N_S++] = ((DL_DATA_Request)p).Data;
            State = State_T.TimerRecovery;
            return;

        L4230:
            UI_Command(((DL_UNIT_DATA_Request)p).Data);
            State = State_T.TimerRecovery;
            return;

        L4280:
            if (OwnReceiverBusy)
                goto L4281;
            OwnReceiverBusy = true;
            F = false;
            OnAX25OutputEvent(new AX25_RNR(m_modulo, N_R, F, false, true));
            AcknowledgePending = false;

        L4281:
            State = State_T.TimerRecovery;
            return;

        L4290:
            if (!OwnReceiverBusy)
                goto L4281;
            OwnReceiverBusy = false;
            P = true;
            OnAX25OutputEvent(new AX25_RR(m_modulo, N_R, P, true, false));
            AcknowledgePending = false;
            if (T1.Running)
                goto L4281;
            T3.Stop();
            T1.Start(T1V);
            goto L4281;
        }

        private void TimerRecovery(LinkMultiplexerPrimitive p)
        {
            switch (p.LinkMultiplexerPrimitiveType)
            {
                case LinkMultiplexerPrimitive_T.LM_SEIZE_Confirm_T:
                    goto L4210;
                default:
                    return;
            } // end switch //


        L4210:
            if (!AcknowledgePending)
                goto L4221;
            AcknowledgePending = false; ;
            EnquiryResponse(false);

        L4221:
            OnLinkMultiplexerOutputEvent(new LM_RELEASE_Request(m_multiplexer));
            State = State_T.TimerRecovery;
            return;
        }

        private void TimerRecovery(AX25Frame f)
        {
            switch (f.FrameType)
            {
                case AX25Frame_T.SABM:
                    goto L4120;
                case AX25Frame_T.SABME:
                    goto L4130;
                case AX25Frame_T.RR:
                    goto L4150;
                case AX25Frame_T.RNR:
                    goto L4160;
                case AX25Frame_T.DISC:
                    goto L4190;
                case AX25Frame_T.UA:
                    goto L4200;
                case AX25Frame_T.UI:
                    goto L4220;
                case AX25Frame_T.REJ:
                    goto L4240;
                case AX25Frame_T.DM:
                    goto L4270;
                case AX25Frame_T.FRMR:
                    goto L4300;
                case AX25Frame_T.SREJ:
                    goto L4310;
                case AX25Frame_T.I:
                    goto L4350;
                default:
                    return;
            } // end switch //

        L4120:
            SetVersion(AX25Version.V2_0);

        L4121:
            F = P;
            OnAX25OutputEvent(new AX25_UA(F, false, true));
            ClearExceptionConditions();
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorF));
            if (V_S == V_A)
                goto L4122;
            IFrameQueue_Clear();
            OnDataLinkOutputEvent(new DL_CONNECT_Indication("<TODO>", m_version));

        L4122:
            T1.Stop();
            T3.Start(T3V);
            V_S = 0;
            V_A = 0;
            V_S = 0;
            State = State_T.Connected;
            return;

        L4130:
            SetVersion(AX25Version.V2_2);
            goto L4121;

        L4141:
            V_A = N_R;
        State = State_T.TimerRecovery;
            return;

        L4150:
            PeerReceiverBusy = false;

        L4151:
            if (f.Response && F)
                goto L4181;
            if (!(f.Command && P))
                goto L4152;
            EnquiryResponse(true);

        L4152:
            if ((LE(V_A, N_R) && LE(N_R, V_S)))
                goto L4141;

        L4153:
            NrErrorRecovery();
            State = State_T.AwaitingConnect;
            return;

        L4160:
            PeerReceiverBusy = true;
            goto L4151;

        L4171:
            InvokeRetransmission();
            State = State_T.TimerRecovery;
            return;

        L4181:
            T1.Stop();
            SelectT1Value();
            if (!(LE(V_A, N_R) && LE(N_R, V_S)))
                goto L4153;
            V_A = N_R;
            if (V_S != V_A)
                goto L4171;
            T3.Start(T3V);
            State = State_T.Connected;
            return;

        L4190:
            IFrameQueue_Clear();
            F = P;
            OnAX25OutputEvent(new AX25_UA(F, false, true));
            OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
            T1.Stop();
            T3.Stop();
            State = State_T.Disconnected;
            return;

        L4200:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorC));
            EstablishDataLink(m_version);
            Layer3Initiated = false;
            State = State_T.AwaitingConnect;
            return;

        L4220:
            UI_Check((AX25_UI)f);
            if (!P)
                goto L4221;
            EnquiryResponse(true);

        L4221:
            State = State_T.TimerRecovery;
            return;

        L4240:
            PeerReceiverBusy = false;
            if (f.Response && F)
                goto L4261;
            if (!(f.Command && P))
                goto L4241;
            EnquiryResponse(true);

        L4241:
            if (!(LE(V_A, N_R) && LE(N_R, V_S)))
                goto L4251;
            V_A = N_R;
            if (V_S != V_A)
                goto L4252;
            State = State_T.TimerRecovery;
            return;

        L4251:
            NrErrorRecovery();
            State = State_T.AwaitingConnect;
            return;

        L4252:
            InvokeRetransmission();
            State = State_T.TimerRecovery;
            return;

        L4261:
            T1.Stop();
            SelectT1Value();
            if (!(LE(V_A, N_R) && LE(N_R, V_S)))
                goto L4251;
            V_A = N_R;
            if (V_S != V_A)
                goto L4252;
            T3.Start(T3V);
            State = State_T.Connected;
            return;

        L4270:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorE));
            OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
            IFrameQueue_Clear();
            T1.Stop();
            T3.Stop();
            State = State_T.Disconnected;
            return;

        L4300:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorK));
            EstablishDataLink(m_version);
            Layer3Initiated = false;
            State = State_T.AwaitingConnect;
            return;

        L4310:
            PeerReceiverBusy = false;
            if (f.Response)
                goto L4331;
            if (!(LE(V_A, N_R) && LE(N_R, V_S)))
                goto L4321;
            if (!P)
                goto L4311;
            V_A = N_R;

        L4311:
            if (V_S != V_A)
                goto L4322;
            State = State_T.TimerRecovery;
            return;

        L4321:
            NrErrorRecovery();
            State = State_T.AwaitingConnect;
            return;

        L4322:
            IFrameQueue[N_S++] = ((AX25_I)f).I;
            State = State_T.TimerRecovery;
            return;

        L4331:
            T1.Stop();
            SelectT1Value();
            if (!(LE(V_A, N_R) && LE(N_R, V_S)))
                goto L4321;
            if (!F)
                goto L4332;
            V_A = N_R;

        L4332:
            if (V_S != V_A)
                goto L4322;
            T3.Start(T3V);
            State = State_T.Connected;
            return;

        L4341:
            NrErrorRecovery();
            State = State_T.AwaitingConnect;
            return;

        L4350:
            if (!f.Command)
                goto L4371;
            if (((AX25_I)f).InfoFieldLength > N1R)
                goto L4361;
            if (!(LE(V_A, N_R) && LE(N_R, V_S)))
                goto L4341;
            V_A = N_R;
            goto L4381;

        L4361:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorO));
            EstablishDataLink(m_version);
            Layer3Initiated = false;
            State = State_T.AwaitingConnect;
            return;

        L4371:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorS));
            /*DiscardIFrame();*/
            State = State_T.TimerRecovery;
            return;

        L4381:
            if (OwnReceiverBusy)
                goto L4441;
            if (N_S != V_R)
                goto L4401;
            V_R++;
            RejectException = false;
            if (SRejectException > 0)
                --SRejectException;
            OnDataLinkOutputEvent(new DL_DATA_Indication(((AX25_I)f).I));

        L4382:
            if (IFrameStore[V_R] == null)
                goto L4391;
            f = IFrameStore[V_R];
            IFrameStore[V_R] = null;
            N_R = ((AX25_I)f).N_R;
            N_S = ((AX25_I)f).N_S;
            P = ((AX25_I)f).P;
            OnDataLinkOutputEvent(new DL_DATA_Indication(((AX25_I)f).I));
            V_R++;
            goto L4382;

        L4391:
            if (P)
                goto L4402;
            if (AcknowledgePending)
                goto L4403;
            OnLinkMultiplexerOutputEvent(new LM_SEIZE_Request(m_multiplexer));
            AcknowledgePending = true;
            goto L4403;

        L4401:
            if (!RejectException)
                goto L4411;
            /*DiscardContentsOfIFrame();*/
            if (!P)
                goto L4403;

        L4402:
            F = true;
            N_R = V_R;
            OnAX25OutputEvent(new AX25_RR(m_modulo, N_R, F, false, true));
            AcknowledgePending = false;

        L4403:
            State = State_T.TimerRecovery;
            return;

        L4411:
            if (SREJEnabled)
                goto L4421;
            /*DiscardContentsOfIFrame();*/
            RejectException = true;
            F = P;
            OnAX25OutputEvent(new AX25_REJ(m_modulo, N_R, F, false, true));

        L4413:
            AcknowledgePending = false;
            goto L4403;

        L4421:
            /*SaveContentsOfIFrame();*/
            if (SRejectException > 0)
                goto L4431;
            if (GT(N_S, V_R + 1))
                goto L4422;
            N_R = V_R;
            F = true;

        L4422:
            ++SRejectException;
            OnAX25OutputEvent(new AX25_SREJ(m_modulo, N_R, F, false, true));
            goto L4413;

        L4431:
            N_R = N_S;
            F = false;
            goto L4422;

        L4441:
            /*DiscardContentsOfIFrame();*/
            if (!P)
                goto L4442;
            F = true;
            N_R = V_R;
            OnAX25OutputExpeditedEvent(new AX25_RNR(m_modulo, N_R, F, false, true));
            AcknowledgePending = false;

        L4442:
            State = State_T.TimerRecovery;
            return;
        }

        private void TimerT1Expiry()
        {
            lock (this)
            {
                switch (State)
                {
                    case State_T.AwaitingConnect:
                        if (RC == m_config.Initial_N2)
                        {
                            IFrameQueue_Clear();
                            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorG));
                            OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
                            State = State_T.Disconnected;
                        }
                        else
                        {
                            RC += 1;
                            switch (m_version)
                            {
                                case AX25Version.V2_0:
                                    OnAX25OutputEvent(new AX25_SABM(true));
                                    break;
                                case AX25Version.V2_2:
                                    OnAX25OutputEvent(new AX25_SABME(true));
                                    break;
                            }
                            SelectT1Value();
                            T1.Start(T1V);
                        }
                        break;
                    case State_T.AwaitingConnect2_2:
                        if (RC == m_config.Initial_N2)
                        {
                            IFrameQueue_Clear();
                            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorG));
                            OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
                            State = State_T.Disconnected;
                        }
                        else
                        {
                            RC += 1;
                            OnAX25OutputEvent(new AX25_SABM(true));
                            SelectT1Value();
                            T1.Start(T1V);
                        }
                        break;
                    case State_T.AwaitingRelease:
                        if (RC == m_config.Initial_N2)
                        {
                            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorH));
                            OnDataLinkOutputEvent(new DL_DISCONNECT_Confirm());
                            State = State_T.Disconnected;
                        }
                        else
                        {
                            RC += 1;
                            OnAX25OutputEvent(new AX25_DISC(true));
                            SelectT1Value();
                            T1.Start(T1V);
                        }
                        break;
                    case State_T.Connected:
                        RC = 1;
                        TransmitEnquiry();
                        State = State_T.TimerRecovery;
                        break;
                    case State_T.TimerRecovery:
                        if (RC == m_config.Initial_N2)
                        {
                            RC += 1;
                            TransmitEnquiry();
                            break;
                        }
                        if (V_A == V_S)
                            if (PeerReceiverBusy)
                                OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorU));
                            else
                                OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorT));
                        else
                            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorI));
                        OnDataLinkOutputEvent(new DL_DISCONNECT_Request());
                        IFrameQueue_Clear();
                        OnAX25OutputEvent(new AX25_DM(F));
                        State = State_T.Disconnected;
                        break;
                    default:
                        break;
                } // end switch //
            } // end lock (this) //
        }

        private void TimerT3Expiry()
        {
            lock (this)
            {
                switch (State)
                {
                    case State_T.Connected:
                        RC = 0;
                        TransmitEnquiry();
                        State = State_T.TimerRecovery;
                        break;
                    default:
                        break;
                } // end switch //
            } // end lock (this) //
        }

        private void UI_Command(byte[] data)
        {
            OnAX25OutputEvent(new AX25_UI(data, false, true, false));
        }

        private void I_Command(byte[] data)
        {
            OnAX25OutputEvent(new AX25_I(data, m_modulo, N_R, N_S, P, true, false));
        }

        private void ControlFieldError() {
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorL));
            if ((State == State_T.Connected) || (State == State_T.TimerRecovery))
            {
                IFrameQueue_Clear();
                EstablishDataLink(m_version);
                Layer3Initiated = true;
                State = State_T.AwaitingConnect;
            }
        }

        private void InfoNotPermittedInFrame()
        {
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorM));
            if ((State == State_T.Connected) || (State == State_T.TimerRecovery))
            {
                IFrameQueue_Clear();
                EstablishDataLink(m_version);
                Layer3Initiated = true;
                State = State_T.AwaitingConnect;
            }
        }

        private void IncorrectUorSFrameLength()
        {
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorN));
            if ((State == State_T.Connected) || (State == State_T.TimerRecovery))
            {
                IFrameQueue_Clear();
                EstablishDataLink(m_version);
                Layer3Initiated = true;
                State = State_T.AwaitingConnect;
            }
        }

        private bool AbleToEstablish()
        {
            return true;
        }

        private void EstablishFromAwait(AX25Modulo modulo)
        {
            m_modulo = (modulo==AX25Modulo.MOD128)?AX25Modulo.MOD128:AX25Modulo.MOD8;
            OnAX25OutputEvent(new AX25_UA(F));
            ClearExceptionConditions();
            V_S = 0;
            V_A = 0;
            V_R = 0;
            OnDataLinkOutputEvent(new DL_CONNECT_Indication("TODO", modulo));
            SRT = m_config.Initial_SRT;
            T1V = 2 * SRT;
            T3.Start(T3V);
            State = State_T.Connected;
        }

        private void EstablishFromRecover(AX25Modulo modulo, bool p)
        {
            m_modulo = (modulo == AX25Modulo.MOD128) ? AX25Modulo.MOD128 : AX25Modulo.MOD8;
            F = p;
            OnAX25OutputEvent(new AX25_UA(F));
            ClearExceptionConditions();
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorF));
            if (V_S != V_A)
            {
                IFrameQueue_Clear();
                OnDataLinkOutputEvent(new DL_CONNECT_Indication("TODO", m_modulo));
            }
            T1.Stop();
            T3.Start(T3V);
            V_S = 0;
            V_A = 0;
            V_R = 0;
            State = State_T.Connected;
        }

        private void FramePopOffQueue(byte[] data)
        {
            switch (State)
            {
                default:
                    break;
            } // end switch //
        }

        private void IFramePopOffQueue(byte[] data)
        {
            switch (State)
            {
                case State_T.Connected:
                case State_T.TimerRecovery:
                    if (PeerReceiverBusy)
                    {
                        IFrameQueue[N_S++] = data;
                        break;
                    }
                    if (V_S == V_A + k)
                    {
                        IFrameQueue[N_S++] = data;
                        break;
                    }
                    N_S = V_S;
                    N_R = V_R;
                    P = false;
                    I_Command(data);
                    V_S++;
                    AcknowledgePending = false;
                    if (!T1.Running)
                    {
                        T3.Stop();
                        T1.Start(T1V);
                    }
                    break;
                case State_T.AwaitingConnect:
                case State_T.AwaitingConnect2_2:
                    if (!Layer3Initiated)
                        IFrameQueue[N_S++] = data;
                    break;
                default:
                    break;
            } // end switch //
        }

        /****************************************************************************/
        /************************** Official subroutines ****************************/
        /****************************************************************************/

        private void NrErrorRecovery()
        {
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorJ));
            EstablishDataLink(m_version);
            Layer3Initiated = false;
        }

        private void EstablishDataLink(AX25Version version)
        {
            ClearExceptionConditions();
            RC = 0;
            if (version == AX25Version.V2_0)
                OnAX25OutputEvent(new AX25_SABM(true));
            else
                OnAX25OutputEvent(new AX25_SABME(true));
            T3.Stop();
            T1.Start(T1V);
        }

        private void ClearExceptionConditions()
        {
            PeerReceiverBusy = false;
            RejectException = false;
            OwnReceiverBusy = false;
            AcknowledgePending = false;
        }

        private void TransmitEnquiry()
        {
            if (OwnReceiverBusy)
                OnAX25OutputEvent(new AX25_RNR(m_modulo, V_R, true, true, false));
            else
                OnAX25OutputEvent(new AX25_RR(m_modulo, V_R, true, true, false));
            AcknowledgePending = false;
            T1.Start(T1V);
        }

        private void EnquiryResponse(bool f)
        {
            if (OwnReceiverBusy)
                OnAX25OutputEvent(new AX25_RNR(m_modulo, V_R, f, false, true));
            else
                OnAX25OutputEvent(new AX25_RR(m_modulo, V_R, f, false, true));
            AcknowledgePending = false;
        }

        private void InvokeRetransmission()
        {
            int x = V_S;
            V_S = N_R;
            do
            {
                OnAX25OutputEvent(new AX25_I(IFrameQueue[V_S], m_modulo, N_R, N_S, true, true, false));
                V_S++;
            } while (V_S != x);
        }

        private void CheckIFrameAcknowledged()
        {
            if (PeerReceiverBusy)
            {
                V_A = N_R;
                T3.Start(T3V);
                if (!T1.Running)
                    T1.Start(T1V);
                return;

            }
            if (N_R == V_S)
            {
                V_A = N_R;
                T1.Stop();
                T3.Start(T3V);
                SelectT1Value();
                return;
            }
            if (N_R != V_A)
            {
                V_A = N_R;
                T1.Start(T1V);
                return;
            }
        }

        private void CheckNeedForResponse(AX25Frame f)
        {
            if (f.Command && P)
            {
                EnquiryResponse(true);
            }
            else
            {
                if ((f.Response) && F)
                    OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorA));
            }
        }

        private void UI_Check(AX25_UI f)
        {
            if (f.Command)
            {
                if (f.InfoFieldLength > m_config.Initial_N1)
                {
                    OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorK));
                }
                else
                {
                    OnDataLinkOutputEvent(new DL_UNIT_DATA_Indication(f.I));
                }
            }
            else
            {
                OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorQ));
            }
        }

        private void SelectT1Value()
        {
            const decimal sevenEigth = 7.0M / 8.0M;
            const decimal oneEigth = 1.0M / 8.0M;

            if (RC == 0)
            {
                SRT = Decimal.ToInt64(Math.Round(
                    sevenEigth * SRT
                    + oneEigth * T1V
                    - oneEigth * T1.RemainingLastStopped));
                T1V = 2 * SRT;
            } else {
                if (!T1.Running)
                    T1V = 2 ^ (RC + 1) * SRT;
            }
        }

        private void SetVersion(AX25Version version)
        {
            switch (version)
            {
                case AX25Version.V2_0:
                    m_modulo = AX25Modulo.MOD8;
                    N1R = 2048;
                    k = 4;
                    T2 = 3000;
                    m_config.Initial_N2 = 10;
                    break;
                case AX25Version.V2_2:
                    m_modulo = AX25Modulo.MOD128;
                    N1R = 2048;
                    k = 32;
                    T2 = 3000;
                    m_config.Initial_N2 = 10;
                    break;
            } // end switch //
        }

        /****************************************************************************/
        /************************** Private subroutines *****************************/
        /****************************************************************************/

        private DL_ERROR_Indication NewDL_ERROR_Indication(ErrorCode_T erc)
        {
            return new DL_ERROR_Indication((long)erc, ErrorCode_N[erc]);
        }

        private bool GT(int op1, int op2)
        {
            int upper = (_v_a + k - 1) % (int)m_modulo;
            if (upper >= _v_a)
            {
                if (op1 > upper)
                    op1 -= (int)m_modulo;
                if (op2 > upper)
                    op2 -= (int)m_modulo;
            }
            else
            {
                if (op1 <= upper)
                    op1 += (int)m_modulo;
                if (op2 <= upper)
                    op2 += (int)m_modulo;
            }
            return (op1 > op2);
        }

        private bool LE(int op1, int op2)
        {
            return (!GT(op1, op2));
        }

        private void IFrameQueue_Clear()
        {
            lock (IFrameQueue)
            {
                for (int i = 0; i < 128; ++i)
                    IFrameQueue[i] = null;
            }
        }

        private void TraceLog(string message)
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, "DataLinkStateMachine", message);
        }

    }
}
