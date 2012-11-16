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
        private int V_S = 0;

        /// <summary>
        /// Send Sequence Number N(S).
        /// The send sequence number is found in the control field of all I frames. It contains the sequence number of the
        /// I frame being sent. Just prior to the transmission of the I frame, N(S) is updated to equel the send state variable.
        /// </summary>
        private int N_S = 0;

        /// <summary>
        /// Receive State Variable V(R).
        /// The receive state variable exists within the TNC. It contains the sequence number of the next expected
        /// received I frame. This variable is updated upon the reception of an error-free I frame whose send sequence
        /// number equals the present received state variable value.
        /// </summary>
        private int V_R = 0;

        /// <summary>
        /// Received Sequence Number N(R).
        /// The received sequence number exists in both I and S frames. Prior to sending an I or S frame, this variable is
        /// updated to equal that of the received state variable, thus implicitly acknowledging the proper reception of all
        /// frames up to and including N(R)-1;
        /// </summary>
        private int N_R = 0;

        /// <summary>
        /// Acknowledge State Variable V(A).
        /// The acknowledge state variable exists within the TNC and is never sent. It contains the sequence number of
        /// the last frame acknowledged by its peer [V(A)-1 equels the N(S) of the last acknowledged I frame].
        /// </summary>
        private int V_A = 0;

        /// <summary>
        /// Window Size Receive.
        /// </summary>
        private int k = 0;

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
        internal AX25Modulo m_modulo = AX25Modulo.MOD8;

        private bool Layer3Initiated = false; // SABM was sent by request of Layer 3; i.e., DL-CONNECT primitive.
        private bool PeerReceiverBusy = false; // Remote station is busy and cannot receive I frames.
        private bool OwnReceiverBusy  = false; // Layer 3 is busy and cannot receive I frames.
        private bool RejectException = false; // A REJ frame has been sent to the remote station.
        private bool SelectiveRejectException = false; // A SREJ fram has been sent to the remote station.
        private bool AcknowledgePending = false; // I frames have been suddenly received but not yet acknowledged to the remote station.
        private long SAT = 100;
        private long SRT = 100; // Smoothed round trip time.
        private long T1V = 100; // Next value for T1; default initial value is initial value of SRT
        private long T3V = 1000;
        private int N1R = 2048;
        private int kR = 4;
        private int T2 = 3000;
        private int RC = 0;
        private int SRejectException;
        private bool F = false; // Final bit
        private bool P = false; // Poll bit
        private bool SREJEnabled;
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
            switch (m_state)
            {
                case State_T.Disconnected: Disconnected(p); break;
                case State_T.Connected: Connected(p); break;
                case State_T.AwaitingConnect: AwaitingConnect(p); break;
                case State_T.AwaitingRelease: AwaitingRelease(p); break;
                case State_T.TimerRecovery: TimerRecovery(p); break;
                case State_T.AwaitingConnect2_2: AwaitingConnect2_2(p); break;
            } // end switch //
        }

        internal void Input(LinkMultiplexerPrimitive p)
        {
            switch (m_state)
            {
                case State_T.Connected: Connected(p); break;
                case State_T.TimerRecovery: TimerRecovery(p); break;
                default: break;
            } // end switch //
        }

        internal void Input(AX25Frame f)
        {
            if (f is AX25_I)
            {
                AX25_I i = (AX25_I)f;
                P = i.P;
                N_R = i.N_R;
                N_S = i.N_S;
            }
            else if (f is AX25SFrame)
            {
                AX25SFrame s = (AX25SFrame)f;
                P = s.PF;
                N_R = s.N_R;
            }
            else if (f is AX25UFrame)
            {
                AX25UFrame u = (AX25UFrame)f;
                P = u.PF;
            }
            switch (m_state)
            {
                case State_T.Disconnected: Disconnected(f); break;
                case State_T.Connected: Connected(f); break;
                case State_T.AwaitingConnect: AwaitingConnect(f); break;
                case State_T.AwaitingRelease: AwaitingRelease(f); break;
                case State_T.TimerRecovery: TimerRecovery(f); break;
                case State_T.AwaitingConnect2_2: AwaitingConnect2_2(f); break;
            } // end switch //
        }

        private void Disconnected(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T :
                    OnDataLinkOutputEvent(new DL_DISCONNECT_Confirm());
                    break;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T :
                    UI_Command(((DL_UNIT_DATA_Request)p).Data);
                    break;
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    this.SAT = m_config.Initial_SAT;
                    this.T1V = 2 * this.SAT;
                    EstablishDataLink(((DL_CONNECT_Request)p).Version);
                    SetLayer3Initiated();
                    m_state = State_T.AwaitingConnect;
                    break;
                default :
                    break;
            } // end switch //
        }

        private void Disconnected(AX25Frame f)
        {
            switch (f.FrameType)
            {
                case AX25Frame_T.UA:
                    OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorC));
                    break;
                case AX25Frame_T.DM:
                    break;
                case AX25Frame_T.UI:
                    UI_Check((AX25_UI)f);
                    if (((AX25_UI)f).PF)
                        OnAX25OutputEvent(new AX25_DM(true));
                    break;
                case AX25Frame_T.SABM:
                    F = ((AX25_SABM)f).PF;
                    if (AbleToEstablish())
                    {
                        SetVersion(AX25Version.V2_0);
                        EstablishFromAwait(AX25Modulo.MOD8);
                    }
                    else // Not able to establish:
                    {
                        OnAX25OutputEvent(new AX25_DM(F));
                    }
                    break;
                case AX25Frame_T.SABME:
                    F = ((AX25_SABME)f).PF;
                    if (AbleToEstablish())
                    {
                        SetVersion(AX25Version.V2_2);
                        EstablishFromAwait(AX25Modulo.MOD128);
                    }
                    else // Not able to establish:
                    {
                        OnAX25OutputEvent(new AX25_DM(F));
                    }
                    break;
                case AX25Frame_T.DISC:
                    F = ((AX25_DISC)f).PF;
                    OnAX25OutputEvent(new AX25_DM(F));
                    break;
                case AX25Frame_T.I:
                    F = ((AX25_I)f).P;
                    OnAX25OutputEvent(new AX25_DM(F));
                    break;
                default:
                    if (f is AX25SFrame)
                        F = ((AX25SFrame)f).PF;
                    else if (f is AX25UFrame)
                        F = ((AX25UFrame)f).PF;
                    else
                        F = false;
                    OnAX25OutputEvent(new AX25_DM(F));
                    break;
            } // end switch //
        }

        private void AwaitingConnect(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    DiscardIFrameQueue();
                    SetLayer3Initiated();
                    break;
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T:
                    Requeue();
                    break;
                case DataLinkPrimitive_T.DL_DATA_Request_T:
                    if (!IsLayer3Initiated())
                        PushFrameOnQueue(((DL_DATA_Request)p).Data);
                    break;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T:
                    UI_Command(((DL_UNIT_DATA_Request)p).Data);
                    break;
                default:
                    break;
            } // end switch //
        }

        private void AwaitingConnect(AX25Frame f)
        {
            switch (f.FrameType)
            {
                case AX25Frame_T.SABM:
                    F = ((AX25_SABM)f).PF;
                    OnAX25OutputEvent(new AX25_UA(F));
                    break;
                case AX25Frame_T.DISC:
                    F = ((AX25_DISC)f).PF;
                    OnAX25OutputEvent(new AX25_DM(F));
                    break;
                case AX25Frame_T.UI:
                    UI_Check((AX25_UI)f);
                    if (((AX25_UI)f).PF)
                        OnAX25OutputEvent(new AX25_DM(true));
                    break;
                case AX25Frame_T.DM:
                    if (((AX25_DM)f).PF)
                    {
                        DiscardIFrameQueue();
                        OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
                        T1.Stop();
                        m_state = State_T.Disconnected;
                    }
                    break;
                case AX25Frame_T.UA:
                    if (((AX25_UA)f).PF)
                    {
                        if (IsLayer3Initiated()) {
                            OnDataLinkOutputEvent(new DL_CONNECT_Confirm());
                        } else {
                            if (V_S != V_A) {
                                DiscardIFrameQueue();
                                OnDataLinkOutputEvent(new DL_CONNECT_Indication("TODO", m_modulo));
                            }
                        }
                        T1.Stop();
                        T3.Stop();
                        SelectT1Value();
                        m_state = State_T.Connected;
                    }
                    else
                    {
                        OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorD));
                    }
                    break;
                case AX25Frame_T.SABME:
                    F = ((AX25_SABME)f).PF;
                    OnAX25OutputEvent(new AX25_DM(F));
                    m_state = State_T.AwaitingConnect2_2;
                    break;
                default:
                    break;
            } // end switch //
        }

        private void AwaitingConnect2_2(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    DiscardIFrameQueue();
                    SetLayer3Initiated();
                    break;
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T:
                    Requeue();
                    break;
                case DataLinkPrimitive_T.DL_DATA_Request_T:
                    if (!IsLayer3Initiated())
                        PushFrameOnQueue(((DL_DATA_Request)p).Data);
                    break;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T:
                    UI_Command(((DL_UNIT_DATA_Request)p).Data);
                    break;
                default:
                    break;
            } // end switch //
        }

        private void AwaitingConnect2_2(AX25Frame f)
        {
            switch (f.FrameType)
            {
                case AX25Frame_T.UI:
                    UI_Check((AX25_UI)f);
                    if (P)
                        OnAX25OutputEvent(new AX25_DM(true));
                    break;
                case AX25Frame_T.SABM:
                    F = P;
                    OnAX25OutputEvent(new AX25_UA(F));
                    m_state = State_T.AwaitingConnect;
                    break;
                case AX25Frame_T.DISC:
                    F = P;
                    OnAX25OutputEvent(new AX25_DM(F));
                    break;
                case AX25Frame_T.DM:
                    if (F)
                    {
                        DiscardIFrameQueue();
                        OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
                        T1.Stop();
                        m_state = State_T.Disconnected;
                    }
                    break;
                case AX25Frame_T.UA:
                    if (!F) {
                        OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorD));
                        break;
                    }
                    if (Layer3Initiated)
                    {
                        OnDataLinkOutputEvent(new DL_CONNECT_Confirm());
                    }
                    else
                    {
                        if (V_S != V_A)
                        {
                            DiscardIFrameQueue();
                            OnDataLinkOutputEvent(new DL_CONNECT_Indication("TODO", m_modulo));
                        }
                    }
                    T1.Stop();
                    T3.Stop();
                    V_S = 0;
                    V_A = 0;
                    V_R = 0;
                    SelectT1Value();
                    m_state = State_T.Connected;
                    break;
                case AX25Frame_T.SABME:
                    F = P;
                    OnAX25OutputEvent(new AX25_UA(F));
                    break;
                case AX25Frame_T.FRMR:
                    SRT = m_config.Initial_SRT;
                    T1V = m_config.Initial_SRT * 2;
                    EstablishDataLink(m_version);
                    SetLayer3Initiated();
                    m_state = State_T.AwaitingConnect;
                    break;
                default:
                    break;
            } // end switch //
        }

        private void AwaitingRelease(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T:
                    OnAX25OutputEvent(new AX25_DM(true));
                    T1.Stop();
                    m_state = State_T.Disconnected;
                    break;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T:
                    UI_Command(((DL_UNIT_DATA_Request)p).Data);
                    break;
                default:
                    break;
            } // end switch //
        }

        private void AwaitingRelease(AX25Frame f)
        {
            switch (f.FrameType)
            {
                case AX25Frame_T.SABM:
                    F = ((AX25_SABM)f).PF;
                    OnAX25OutputEvent(new AX25_DM(F));
                    break;
                case AX25Frame_T.DISC:
                    F = ((AX25_DISC)f).PF;
                    OnAX25OutputEvent(new AX25_UA(F));
                    break;
                case AX25Frame_T.I:
                    if (((AX25_I)f).P)
                        OnAX25OutputEvent(new AX25_DM(true));
                    break;
                case AX25Frame_T.RR:
                case AX25Frame_T.RNR:
                case AX25Frame_T.REJ:
                case AX25Frame_T.SREJ:
                    if (((AX25SFrame)f).PF)
                        OnAX25OutputEvent(new AX25_DM(true));
                    break;
                case AX25Frame_T.UI:
                    UI_Check((AX25_UI)f);
                    if (((AX25_UI)f).PF)
                        OnAX25OutputEvent(new AX25_DM(true));
                    break;
                case AX25Frame_T.UA:
                    if (((AX25_UA)f).PF) {
                        OnDataLinkOutputEvent(new DL_DISCONNECT_Confirm());
                        T1.Stop();
                        m_state = State_T.Disconnected;
                    } else {
                        OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorD));
                    }
                    break;
                case AX25Frame_T.DM:
                    if (((AX25_DM)f).PF) {
                        OnDataLinkOutputEvent(new DL_DISCONNECT_Confirm());
                        T1.Stop();
                        m_state = State_T.Disconnected;
                    }
                    break;
                default:
                    break;
            } // end switch //
        }

        private void Connected(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    DiscardIFrameQueue();
                    EstablishDataLink(m_version);
                    SetLayer3Initiated();
                    m_state = State_T.AwaitingConnect;
                    break;
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T:
                    DiscardIFrameQueue();
                    RC = 0;
                    OnAX25OutputEvent(new AX25_DISC(true));
                    T3.Stop();
                    T1.Start(T1V);
                    m_state = State_T.AwaitingRelease;
                    break;
                case DataLinkPrimitive_T.DL_DATA_Request_T:
                    PushOnIFrameQueue(((DL_DATA_Request)p).Data);
                    break;
                case DataLinkPrimitive_T.DL_FLOW_OFF_Request_T:
                    if (!OwnReceiverBusy)
                    {
                        OwnReceiverBusy = true;
                        OnAX25OutputEvent(new AX25_RNR(m_modulo, N_R, false));
                        AcknowledgePending = false;;
                    }
                    break;
                case DataLinkPrimitive_T.DL_FLOW_ON_Request_T:
                    if (OwnReceiverBusy)
                    {
                        OwnReceiverBusy = false;
                        OnAX25OutputEvent(new AX25_RR(m_modulo, N_R, true));
                        AcknowledgePending = false;;
                        if (!T1.Running)
                        {
                            T3.Stop();
                            T1.Start(T1V);
                        }
                    }
                    break;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T:
                    UI_Command(((DL_UNIT_DATA_Request)p).Data);
                    break;
                default:
                    break;
            } // end switch //
        }

        private void Connected(LinkMultiplexerPrimitive p)
        {
            switch (p.LinkMultiplexerPrimitiveType)
            {
                case LinkMultiplexerPrimitive_T.LM_SEIZE_Confirm_T:
                    if (AcknowledgePending)
                    {
                        AcknowledgePending = false;;
                        EnquiryResponse(false);
                    }
                    OnLinkMultiplexerOutputEvent(new LM_RELEASE_Request(m_multiplexer));
                    break;
                default:
                    break;
            } // end switch //
        }

        private void Connected(AX25Frame f)
        {
            switch (f.FrameType)
            {
                case AX25Frame_T.SABM:
                case AX25Frame_T.SABME:
                    F = ((AX25UFrame)f).PF;
                    OnAX25OutputEvent(new AX25_UA(F));
                    ClearExceptionConditions();
                    OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorF));
                    if (V_S != V_A) {
                        DiscardIFrameQueue();
                        OnDataLinkOutputEvent(new DL_CONNECT_Indication("TODO", m_modulo));
                    }
                    T1.Stop();
                    T3.Start(T3V);
                    break;
                case AX25Frame_T.DISC:
                    DiscardIFrameQueue();
                    F = ((AX25_DISC)f).PF;
                    OnAX25OutputEvent(new AX25_UA(F));
                    OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
                    T1.Stop();
                    T3.Stop();
                    m_state = State_T.Disconnected;
                    break;
                case AX25Frame_T.UA:
                    if (!m_config.Initial_relaxed) // Some implementations sends spurios UA's
                    {
                        OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorC));
                        EstablishDataLink(m_version);
                        ClearLayer3Initiated();
                        m_state = State_T.AwaitingConnect;
                    }
                    break;
                case AX25Frame_T.DM:
                    OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorE));
                    OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
                    DiscardIFrameQueue();
                    T1.Stop();
                    T3.Stop();
                    m_state = State_T.Disconnected;
                    break;
                case AX25Frame_T.FRMR:
                    OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorK));
                    EstablishDataLink(m_version);
                    ClearLayer3Initiated();
                    m_state = State_T.AwaitingConnect;
                    break;
                case AX25Frame_T.UI:
                    UI_Check((AX25_UI)f);
                    if (((AX25_UI)f).PF)
                        EnquiryResponse(true);
                    break;
                case AX25Frame_T.RR:
                    PeerReceiverBusy = false;
                    CheckNeedForResponse(f);
                    if ((V_A <= N_R) && (N_R <= V_S))
                    {
                        CheckIFrameAcknowledged();
                    }
                    else
                    {
                        NrErrorRecovery();
                        m_state = State_T.AwaitingConnect;
                    }
                    break;
                case AX25Frame_T.RNR:
                    PeerReceiverBusy = true;
                    CheckNeedForResponse(f);
                    if ((V_A <= N_R) && (N_R <= V_S))
                    {
                        CheckIFrameAcknowledged();
                    }
                    else
                    {
                        NrErrorRecovery();
                        m_state = State_T.AwaitingConnect;
                    }
                    break;
                case AX25Frame_T.SREJ:
                    PeerReceiverBusy = false;
                    CheckNeedForResponse(f);
                    if ((V_A <= N_R) && (N_R <= V_S))
                    {
                        if (((AX25_SREJ)f).PF)
                            V_A = N_R;
                        T1.Stop();
                        T3.Start(T3V);
                        SelectT1Value();
                        PushOldIFrameOnQueue();
                    }
                    else
                    {
                        NrErrorRecovery();
                        m_state = State_T.AwaitingConnect;
                    }
                    break;
                case AX25Frame_T.REJ:
                    PeerReceiverBusy = false;
                    CheckNeedForResponse(f);
                    if ((V_A > N_R) || (N_R > V_S))
                    {
                        NrErrorRecovery();
                        m_state = State_T.AwaitingConnect;
                        break;
                    }
                    V_A = N_R;
                    T1.Stop();
                    T3.Stop();
                    SelectT1Value();
                    InvokeRetransmission();
                    break;
                case AX25Frame_T.I:
                    goto L3210;
                default:
                    break;
            } // end switch //
            return;

        L3261:
            NrErrorRecovery();
            m_state = State_T.AwaitingConnect;
            return;

        L3210:
            if (!((AX25_I)f).Command)
                goto L3291;
            if (((AX25_I)f).InfoFieldLength > m_config.Initial_N1)
                goto L3281;
            if ((V_A > N_R) || (N_R > V_S))
                goto L3261;
            CheckIFrameAcknowledged();
            goto L3300;

        L3281:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorO));
            EstablishDataLink(m_version);
            Layer3Initiated = false;
            m_state = State_T.AwaitingConnect;
            return;

        L3291:
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorS));
            /*DiscardIframe()*/
            m_state = State_T.Connected;
            return;

        L3300:
            if (OwnReceiverBusy)
                goto L3361;
            if (N_S != V_R)
                goto L3321;
            V_R = V_R + 1;
            RejectException = false;
            if (SRejectException > 0)
                --SRejectException;
            OnDataLinkOutputEvent(new DL_DATA_Indication(((AX25_I)f).I));

        L3301:
            if (IFrameStore[V_R] == null)
                goto L3311;
            f = IFrameStore[V_R];
            OnDataLinkOutputEvent(new DL_DATA_Indication(((AX25_I)f).I));
            V_R = V_R + 1;
            goto L3301;

        L3311:
            if (P)
                goto L3322;
            if (AcknowledgePending)
                goto L3323;
            OnLinkMultiplexerOutputEvent(new LM_SEIZE_Request(m_multiplexer));
            AcknowledgePending = true;
            goto L3323;

        L3321:
            if (!RejectException)
                goto L3331;
            /*DiscardContentsOfIFrame();*/
            if (!P)
                goto L3323;

        L3322:
            F = true;
            OnAX25OutputEvent(new AX25_RR(m_modulo, N_R, F));
            AcknowledgePending = false;

        L3323:
            m_state = State_T.Connected;
            return;

        L3331:
            if (SREJEnabled)
                goto L3341;

        L3332:
            /*DiscardContentsOfIFrame();*/
            RejectException = true;
            F = P;
            OnAX25OutputEvent(new AX25_REJ(m_modulo, N_R, F));

        L3333:
            AcknowledgePending = false;
            goto L3323;

        L3341:
            IFrameStore[N_R] = (AX25_I)f;
            if (SRejectException > 0)
                goto L3351;
            if (N_S > V_R + 1)
                goto L3332;
            N_R = V_R;
            F = true;

        L3342:
            ++SRejectException;
            OnAX25OutputEvent(new AX25_SREJ(m_modulo, N_R, F));
            goto L3333;

        L3351:
            N_R = N_S;
            F = false;
            goto L3342;

        L3361:
            /*DiscardContentsOfIFrame();*/
            if (!P)
                goto L3362;
            F = true;
            OnAX25OutputEvent(new AX25_RNR(m_modulo, N_R, F));
            AcknowledgePending = false;

        L3362:
            m_state = State_T.Connected;
            return;

        }

        private void TimerRecovery(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType) {
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    DiscardIFrameQueue();
                    EstablishDataLink(m_version);
                    SetLayer3Initiated();
                    m_state = State_T.AwaitingConnect;
                    break;
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T:
                    DiscardIFrameQueue();
                    RC = 0;
                    OnAX25OutputEvent(new AX25_DISC(true));
                    T3.Stop();
                    T1.Start(T1V);
                    m_state = State_T.AwaitingRelease;
                    break;
                case DataLinkPrimitive_T.DL_DATA_Request_T:
                    PushOnIFrameQueue(((DL_DATA_Request)p).Data);
                    break;
                case DataLinkPrimitive_T.DL_FLOW_OFF_Request_T:
                    if (!OwnReceiverBusy)
                    {
                        OwnReceiverBusy = true;
                        OnAX25OutputEvent(new AX25_RNR(m_modulo, N_R, false));
                        AcknowledgePending = false;;
                    }
                    break;
                case DataLinkPrimitive_T.DL_FLOW_ON_Request_T:
                    if (OwnReceiverBusy)
                    {
                        OwnReceiverBusy = false;
                        OnAX25OutputEvent(new AX25_RR(m_modulo, N_R, true));
                        AcknowledgePending = false;;
                        if (!T1.Running)
                        {
                            T3.Stop();
                            T1.Start(T1V);
                        }
                    }
                    break;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T:
                    UI_Command(((DL_UNIT_DATA_Request)p).Data);
                    break;
                default:
                    break;
            } // end switch //
        }

        private void TimerRecovery(LinkMultiplexerPrimitive p)
        {
            switch (p.LinkMultiplexerPrimitiveType)
            {
                case LinkMultiplexerPrimitive_T.LM_SEIZE_Confirm_T:
                    if (AcknowledgePending)
                    {
                        AcknowledgePending = false;;
                        EnquiryResponse(false);
                    }
                    OnLinkMultiplexerOutputEvent(new LM_SEIZE_Request(m_multiplexer));
                    break;
                default:
                    break;
            } // end switch //
        }

        private void TimerRecovery(AX25Frame f)
        {
            switch (f.FrameType)
            {
                case AX25Frame_T.SABM:
                    SetVersion(AX25Version.V2_0);
                    EstablishFromRecover(AX25Modulo.MOD8, ((AX25_SABM)f).PF);
                    break;
                case AX25Frame_T.SABME:
                    SetVersion(AX25Version.V2_2);
                    EstablishFromRecover(AX25Modulo.MOD128, ((AX25_SABM)f).PF);
                    break;
                case AX25Frame_T.RR:
                case AX25Frame_T.RNR:
                    if (f.FrameType == AX25Frame_T.RR)
                        PeerReceiverBusy = false;
                    else
                        PeerReceiverBusy = true;
                    if ((!((AX25SFrame)f).PF) && F) {
                        T1.Stop();
                        SelectT1Value();
                        if ((V_A <= N_R) && (N_R <= V_S)) {
                            V_A = N_R;
                            if (V_S == V_A) {
                                T3.Start(T3V);
                                m_state = State_T.Connected;
                                break;
                            }
                            InvokeRetransmission();
                                break;
                        }
                        NrErrorRecovery();
                        m_state = State_T.AwaitingConnect;
                        break;
                    }
                    if ((((AX25SFrame)f).PF) && P)
                        EnquiryResponse(true);
                    if ((V_A <= N_R) && (N_R <= V_S)) {
                        V_A = N_R;
                        break;
                    }
                    NrErrorRecovery();
                    m_state = State_T.AwaitingConnect;
                    break;
                case AX25Frame_T.DISC:
                    DiscardIFrameQueue();
                    F = P;
                    OnAX25OutputEvent(new AX25_UA(F));
                    OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
                    T1.Stop();
                    T3.Stop();
                    m_state = State_T.Disconnected;
                    break;
                case AX25Frame_T.UA:
                    if (!m_config.Initial_relaxed) // Some implementations sends spurios UA's
                    {
                        OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorC));
                        EstablishDataLink(m_version);
                        ClearLayer3Initiated();
                        m_state = State_T.AwaitingConnect;
                    }
                    break;
                case AX25Frame_T.UI:
                    UI_Check((AX25_UI)f);
                    if (P)
                        EnquiryResponse(true);
                    break;
                case AX25Frame_T.REJ:
                    PeerReceiverBusy = false;
                    if ((!((AX25_REJ)f).PF) && F)
                    {
                        T1.Stop();
                        SelectT1Value();
                        if ((V_A <= N_R) && (N_R <= V_S))
                        {
                            V_A = N_R;
                            if (V_S == V_A)
                            {
                                T3.Start(T3V);
                                m_state = State_T.Connected;
                                break;
                            }
                            InvokeRetransmission();
                            break;
                        }
                        NrErrorRecovery();
                        m_state = State_T.AwaitingConnect;
                        break;
                    }
                    V_A = N_R;
                    if (V_S != V_A)
                        InvokeRetransmission();
                    break;
                case AX25Frame_T.DM:
                    OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorK));
                    EstablishDataLink(m_version);
                    ClearLayer3Initiated();
                    m_state = State_T.AwaitingConnect;
                    break;
                case AX25Frame_T.SREJ:
                    PeerReceiverBusy = false;
                    if (!((AX25_SREJ)f).PF) {
                        T1.Stop();
                        SelectT1Value();
                        if ((V_A > N_R) || (N_R > V_S)) {
                            NrErrorRecovery();
                            m_state = State_T.AwaitingConnect;
                            break;
                        }
                        if (F)
                            V_A = N_R;
                        if (V_S == V_A) {
                            T3.Start(T3V);
                            m_state = State_T.Connected;
                            break;
                        }
                        PushIFrameOnQueue();
                        break;
                    }
                    if ((V_A > N_R) || (N_R > V_S)) {
                        NrErrorRecovery();
                        m_state = State_T.AwaitingConnect;
                        break;
                    }
                    if (P)
                        V_A = N_R;
                    if (V_S != V_A)
                        PushIFrameOnQueue();
                    break;
                case AX25Frame_T.I:
                    if (!((AX25_I)f).Command)
                    {
                        OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorS));
                        break;
                    }
                    if (((AX25_I)f).InfoFieldLength > m_config.Initial_N1)
                    {
                        OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorO));
                        EstablishDataLink(m_version);
                        ClearLayer3Initiated();
                        m_state = State_T.AwaitingConnect;
                        break;
                    }
                    if ((V_A > N_R) || (N_R > V_S))
                    {
                        NrErrorRecovery();
                        m_state = State_T.AwaitingConnect;
                        break;
                    }
                    V_A = N_R;
                    if (OwnReceiverBusy)
                    {
                        IFrameStore[V_R] = null;
                        if (P)
                        {
                            F = true;
                            N_R = V_R;
                            OnAX25OutputEvent(new AX25_RNR(m_modulo, N_R, F));
                            AcknowledgePending = false;;
                        }
                        break;
                    }
                    if (N_S == V_R) {
                        V_R += 1;
                        RejectException = false;
                        SRejectException = 0;
                        if (SRejectException > 0)
                            SRejectException -= 1;
                        OnDataLinkOutputEvent(new DL_DATA_Indication(((AX25_I)f).I));
                        while (IFrameStore[V_R] != null) {
                            OnDataLinkOutputEvent(new DL_DATA_Indication(IFrameStore[V_R].I));
                            V_R += 1;
                        } // end while //
                        if (!P) {
                            if (!AcknowledgePending) {
                                OnLinkMultiplexerOutputEvent(new LM_SEIZE_Request(m_multiplexer));
                                AcknowledgePending = true;
                            }
                            break;
                        }
                        F = true;
                        N_R = V_R;
                        OnAX25OutputEvent(new AX25_RR(m_modulo, N_R, F));
                        AcknowledgePending = false;;
                        break;
                    }
                    if (RejectException) {
                        IFrameStore[V_R] = null;
                        if (P) {
                            F = true;
                            N_R = V_R;
                            OnAX25OutputEvent(new AX25_RR(m_modulo, N_R, F));
                        }
                        break;
                    }
                    if (!SREJEnabled) {
                        IFrameStore[V_R] = null;
                        if (P) {
                            RejectException = true;
                            SRejectException = 0;
                            F = P;
                            N_R = V_R;
                            OnAX25OutputEvent(new AX25_REJ(m_modulo, N_R, F));
                            AcknowledgePending = false;;
                        }
                        break;
                    }
                    IFrameStore[V_R] = (AX25_I)f;
                    if (SRejectException > 0) {
                        N_R = N_S;
                        F = false;
                    } else {
                        if (N_S > V_R + 1) {
                            IFrameStore[V_R] = null;
                            RejectException = true;
                            SRejectException = 0;
                            F = P;
                            OnAX25OutputEvent(new AX25_REJ(m_modulo, N_R, F));
                            AcknowledgePending = false;;
                            break;
                        }
                        N_R = V_R;
                        F = true;
                    }
                    SRejectException += 1;
                    OnAX25OutputEvent(new AX25_SREJ(m_modulo, N_R, F));
                    AcknowledgePending = false;;
                    break;
                default:
                    break;
            } // end switch //
        }

        private void TimerT1Expiry()
        {
            switch (m_state)
            {
                case State_T.AwaitingConnect:
                    if (RC == m_config.Initial_N2)
                    {
                        DiscardIFrameQueue();
                        OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorG));
                        OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
                        m_state = State_T.Disconnected;
                    }
                    else
                    {
                        RC += 1;
                        switch (m_version)
                        {
                            case AX25Version.V2_0 :
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
                        DiscardIFrameQueue();
                        OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorG));
                        OnDataLinkOutputEvent(new DL_DISCONNECT_Indication());
                        m_state = State_T.Disconnected;
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
                        m_state = State_T.Disconnected;
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
                    m_state = State_T.TimerRecovery;
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
                    DiscardIFrameQueue();
                    OnAX25OutputEvent(new AX25_DM(F));
                    m_state = State_T.Disconnected;
                    break;
                default:
                    break;
            } // end switch //
        }

        private void TimerT3Expiry()
        {
            switch (m_state)
            {
                case State_T.Connected:
                    RC = 0;
                    TransmitEnquiry();
                    m_state = State_T.TimerRecovery;
                    break;
                default:
                    break;
            } // end switch //
        }

        private void UI_Command(byte[] data)
        {
        }

        private void I_Command(byte[] data)
        {
        }

        private void SetLayer3Initiated()
        {
            Layer3Initiated = true;
        }

        private void ClearLayer3Initiated()
        {
            Layer3Initiated = false;
        }

        private bool IsLayer3Initiated()
        {
            return Layer3Initiated;
        }

        private void ControlFieldError() {
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorL));
            if ((m_state == State_T.Connected) || (m_state == State_T.TimerRecovery))
            {
                DiscardIFrameQueue();
                EstablishDataLink(m_version);
                SetLayer3Initiated();
                m_state = State_T.AwaitingConnect;
            }
        }

        private void InfoNotPermittedInFrame()
        {
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorM));
            if ((m_state == State_T.Connected) || (m_state == State_T.TimerRecovery))
            {
                DiscardIFrameQueue();
                EstablishDataLink(m_version);
                SetLayer3Initiated();
                m_state = State_T.AwaitingConnect;
            }
        }

        private void IncorrectUorSFrameLength()
        {
            OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorN));
            if ((m_state == State_T.Connected) || (m_state == State_T.TimerRecovery))
            {
                DiscardIFrameQueue();
                EstablishDataLink(m_version);
                SetLayer3Initiated();
                m_state = State_T.AwaitingConnect;
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
            m_state = State_T.Connected;
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
                DiscardIFrameQueue();
                OnDataLinkOutputEvent(new DL_CONNECT_Indication("TODO", m_modulo));
            }
            T1.Stop();
            T3.Start(T3V);
            V_S = 0;
            V_A = 0;
            V_R = 0;
            m_state = State_T.Connected;
        }

        private void DiscardIFrameQueue()
        {
        }

        private void PushOnIFrameQueue(byte[] data)
        {
        }

        private void PushIFrameOnQueue(byte[] data)
        {
        }

        private void Requeue()
        {
        }

        private void PushFrameOnQueue(byte[] data)
        {
        }

        private void FramePopOffQueue(byte[] data)
        {
            switch (m_state)
            {
                case State_T.AwaitingConnect:
                    if (!Layer3Initiated)
                        PushFrameOnQueue(data);
                    break;
                default:
                    break;
            } // end switch //
        }

        private void IFramePopOffQueue(byte[] data)
        {
            switch (m_state)
            {
                case State_T.Connected:
                case State_T.TimerRecovery:
                    if (PeerReceiverBusy)
                    {
                        PushIFrameOnQueue(data);
                        break;
                    }
                    if (V_S == V_A + k)
                    {
                        PushIFrameOnQueue(data);
                        break;
                    }
                    N_S = V_S;
                    N_R = V_R;
                    P = false;
                    I_Command(data);
                    V_S += 1;
                    AcknowledgePending = false;;
                    if (!T1.Running)
                    {
                        T3.Stop();
                        T1.Start(T1V);
                    }
                    break;
                case State_T.AwaitingConnect2_2:
                    if (!Layer3Initiated)
                        PushIFrameOnQueue(data);
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
            ClearLayer3Initiated();
        }

        private void EstablishDataLink(AX25Version version)
        {
            ClearExceptionConditions();
            RC = 0;
            P = true;
            if (version == AX25Version.V2_0)
                OnAX25OutputEvent(new AX25_SABM(P));
            else
                OnAX25OutputEvent(new AX25_SABME(P));
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
            P = true;
            N_R = V_R;
            if (OwnReceiverBusy)
                OnAX25OutputEvent(new AX25_RNR(m_modulo, N_R, P));
            else
                OnAX25OutputEvent(new AX25_RR(m_modulo, N_R, P));
            AcknowledgePending = false;
            T1.Start(T1V);
        }

        private void EnquiryResponse(bool pf)
        {
            N_R = V_R;
            if (OwnReceiverBusy)
                OnAX25OutputEvent(new AX25_RNR(m_modulo, N_R, pf));
            else
                OnAX25OutputEvent(new AX25_RR(m_modulo, N_R, pf));
            AcknowledgePending = false;
        }

        private void InvokeRetransmission()
        {
            Backtrack();
            int x = V_S;
            V_S = N_R;
            do
            {
                PushOldIFrameOnQueue();
                V_S += 1;
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
                EnquiryResponse(true);
            else
                if ((f.Response) && F)
                    OnDataLinkOutputEvent(NewDL_ERROR_Indication(ErrorCode_T.ErrorA));
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
                    kR = 4;
                    T2 = 3000;
                    m_config.Initial_N2 = 10;
                    break;
                case AX25Version.V2_2:
                    m_modulo = AX25Modulo.MOD128;
                    N1R = 2048;
                    kR = 32;
                    T2 = 3000;
                    m_config.Initial_N2 = 10;
                    break;
            } // end switch //
        }

        /****************************************************************************/
        /************************** Private subroutines *****************************/
        /****************************************************************************/

        private void Backtrack()
        {
        }

        private void PushOldIFrameOnQueue()
        {
        }

        private void PushIFrameOnQueue()
        {
        }

        private DL_ERROR_Indication NewDL_ERROR_Indication(ErrorCode_T erc)
        {
            return new DL_ERROR_Indication((long)erc, ErrorCode_N[erc]);
        }

    }
}
