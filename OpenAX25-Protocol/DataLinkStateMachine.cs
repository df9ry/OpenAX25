using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OpenAX25_Protocol
{

    internal class DataLinkStateMachine
    {
        public enum State_T
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

        public static string GetStateName(State_T state)
        {
            return State_N[state];
        }

        public State_T State {
            get {
                return m_state;
            }
        }

        public string StateName {
            get {
                return State_N[m_state];
            }
        }

        public enum ErrorCode_T
        {
            ErrorA = 'A', ErrorB = 'B', ErrorC = 'C', ErrorD = 'D', ErrorE = 'E', ErrorF = 'F',
            ErrorG = 'G', ErrorH = 'H', ErrorI = 'I', ErrorJ = 'J', ErrorK = 'K', ErrorL = 'L', 
            ErrorM = 'M', ErrorN = 'N', ErrorO = 'O', ErrorP = 'P', ErrorQ = 'Q', ErrorR = 'R',
            ErrorS = 'S', ErrorT = 'T', ErrorU = 'U', ErrorV = 'V'
        }

        private readonly static IDictionary<ErrorCode_T, string> ErrorCode_N = new Dictionary<ErrorCode_T, string>
        {
            { ErrorCode_T.ErrorA, "F=1 received but P=1 not outstanding" },
            { ErrorCode_T.ErrorB, "Unexpected DM with F=! instates 3, 4 or 5" },
            { ErrorCode_T.ErrorC, "Unexcepcted UA in states 3, 4 or 5" },
            { ErrorCode_T.ErrorD, "UA received without F=1 when SABM or DISC was sent P=1" },
            { ErrorCode_T.ErrorE, "DM received in states 3, 4 or 5" },
            { ErrorCode_T.ErrorF, "Data link reset; i.e., SABM received in state 3, 4 or 5" },
            { ErrorCode_T.ErrorG, "Too many retries" },
            { ErrorCode_T.ErrorH, "Too many retries" },
            { ErrorCode_T.ErrorI, "N2 timouts; unacknowledged data" },
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

        public static string ErrorCodeName(ErrorCode_T errorCode)
        {
            return ErrorCode_N[errorCode];
        }

        public enum Version_T {
            V2_0, V2_2
        }

        private Version_T m_version = Version_T.V2_0;

        public Version_T Version
        {
            get
            {
                return m_version;
            }
        }

        private readonly static IDictionary<Version_T, string> Version_N = new Dictionary<Version_T, string>
        {
            { Version_T.V2_0, "V2.0" },
            { Version_T.V2_2, "V2.2" }
        };

        public static string VersionName(Version_T version)
        {
            return Version_N[version];
        }

        private AX25Modulo m_modulo = AX25Modulo.UNSPECIFIED;

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
        private int N1 = 255; // Maximum number of octets in the information field of a frame, excluding inserted 0-bits
        private int N2 = 16; // Maximum number of retries permitted
        private int Vs = 0;
        private int Va = 0;
        private int Vr = 0;
        private int RC = 0;
        private int k = 0;
        private int p = 0;
        private int Ns = 0;
        private int Nr = 0;
        private int SRejectException;
        private bool F = false; // Final bit
        private bool SREJEnabled;
        private readonly AX25Timer T1; // Outstanding I frame or P-bit.
        private readonly AX25Timer T3; // Idle supervision (keep alive).
        private readonly LinkMultiplexerStateMachine m_multiplexer;

        private static void OnT1Callback(object obj)
        {
            ((DataLinkStateMachine)obj).TimerT1Expiry();
        }

        private static void OnT3Callback(object obj)
        {
            ((DataLinkStateMachine)obj).TimerT3Expiry();
        }

        private AX25_Configuration m_config;

        internal DataLinkStateMachine(AX25_Configuration config)
        {
            m_config = config;
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
                default: break;
            } // end switch //
        }

        internal void Input(AX25Frame f)
        {
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

        private void Output(DataLinkPrimitive p)
        {
            ///TODO: Do something with p
        }

        private void Output(LinkMultiplexerPrimitive p)
        {
            ///TODO: Do something with p
        }

        private void Output(AX25Frame f)
        {
            ///TODO: Do something with f
        }

        private void Disconnected(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T :
                    Output(new DL_DISCONNECT_Confirm(this));
                    break;
                case DataLinkPrimitive_T.DL_UNIT_DATA_Request_T :
                    UI_Command(((DL_UNIT_DATA_Request)p).Data);
                    break;
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    this.SAT = m_config.SAT;
                    this.T1V = 2 * this.SAT;
                    EstablishDataLink();
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
                    Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorC));
                    break;
                case AX25Frame_T.DM:
                    break;
                case AX25Frame_T.UI:
                    UI_Check((AX25_UI)f);
                    if (((AX25_UI)f).PF)
                        Output(new AX25_DM(true));
                    break;
                case AX25Frame_T.SABM:
                    F = ((AX25_SABM)f).PF;
                    if (AbleToEstablish())
                    {
                        m_version = Version_T.V2_0;
                        EstablishFromAwait(AX25Modulo.MOD8);
                    }
                    else // Not able to establish:
                    {
                        Output(new AX25_DM(F));
                    }
                    break;
                case AX25Frame_T.SABME:
                    F = ((AX25_SABME)f).PF;
                    if (AbleToEstablish())
                    {
                        m_version = Version_T.V2_2;
                        EstablishFromAwait(AX25Modulo.MOD128);
                    }
                    else // Not able to establish:
                    {
                        Output(new AX25_DM(F));
                    }
                    break;
                case AX25Frame_T.DISC:
                    F = ((AX25_DISC)f).PF;
                    Output(new AX25_DM(F));
                    break;
                case AX25Frame_T.I:
                    F = ((AX25_I)f).P;
                    Output(new AX25_DM(F));
                    break;
                default:
                    if (f is AX25SFrame)
                        F = ((AX25SFrame)f).PF;
                    else if (f is AX25UFrame)
                        F = ((AX25UFrame)f).PF;
                    else
                        F = false;
                    Output(new AX25_DM(F));
                    break;
            } // end switch //
        }

        private void AwaitingConnect(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType)
            {
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    DiscardQueue();
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
                    Output(new AX25_UA(F));
                    break;
                case AX25Frame_T.DISC:
                    F = ((AX25_DISC)f).PF;
                    Output(new AX25_DM(F));
                    break;
                case AX25Frame_T.UI:
                    UI_Check((AX25_UI)f);
                    if (((AX25_UI)f).PF)
                        Output(new AX25_DM(true));
                    break;
                case AX25Frame_T.DM:
                    if (((AX25_DM)f).PF)
                    {
                        DiscardFrameQueue();
                        Output(new DL_DISCONNECT_Indication(this));
                        T1.Stop();
                        m_state = State_T.Disconnected;
                    }
                    break;
                case AX25Frame_T.UA:
                    if (((AX25_UA)f).PF)
                    {
                        if (IsLayer3Initiated()) {
                            Output(new DL_CONNECT_Confirm(this));
                        } else {
                            if (Vs != Va) {
                                DiscardQueue();
                                Output(new DL_CONNECT_Indication(this, m_modulo));
                            }
                        }
                        T1.Stop();
                        T3.Stop();
                        SelectT1Value();
                        m_state = State_T.Disconnected;
                    }
                    else
                    {
                        Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorD));
                    }
                    break;
                case AX25Frame_T.SABME:
                    F = ((AX25_SABME)f).PF;
                    Output(new AX25_DM(F));
                    m_state = State_T.AwaitingConnect2_2;
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
                    Output(new AX25_DM(true));
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
                    Output(new AX25_DM(F));
                    break;
                case AX25Frame_T.DISC:
                    F = ((AX25_DISC)f).PF;
                    Output(new AX25_UA(F));
                    break;
                case AX25Frame_T.I:
                    if (((AX25_I)f).P)
                        Output(new AX25_DM(true));
                    break;
                case AX25Frame_T.RR:
                case AX25Frame_T.RNR:
                case AX25Frame_T.REJ:
                case AX25Frame_T.SREJ:
                    if (((AX25SFrame)f).PF)
                        Output(new AX25_DM(true));
                    break;
                case AX25Frame_T.UI:
                    UI_Check((AX25_UI)f);
                    if (((AX25_UI)f).PF)
                        Output(new AX25_DM(true));
                    break;
                case AX25Frame_T.UA:
                    if (((AX25_UA)f).PF) {
                        Output(new DL_DISCONNECT_Confirm(this));
                        T1.Stop();
                        m_state = State_T.Disconnected;
                    } else {
                        Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorD));
                    }
                    break;
                case AX25Frame_T.DM:
                    if (((AX25_DM)f).PF) {
                        Output(new DL_DISCONNECT_Confirm(this));
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
                    Error();
                    break;
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T:
                    DiscardIFrameQueue();
                    RC = 0;
                    Output(new AX25_DISC(true));
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
                        SetOwnReceiverBusy();
                        Output(new AX25_RNR(m_modulo, Nr, false));
                        ClearAcknowledgePending();
                    }
                    break;
                case DataLinkPrimitive_T.DL_FLOW_ON_Request_T:
                    if (OwnReceiverBusy)
                    {
                        ClearOwnReceiverBusy();
                        Output(new AX25_RR(m_modulo, Nr, true));
                        ClearAcknowledgePending();
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
                        ClearAcknowledgePending();
                        EnquiryResponse(false);
                    }
                    Output(new LM_RELEASE_Request(m_multiplexer));
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
                    Output(new AX25_UA(F));
                    ClearExceptionConditions();
                    Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorF));
                    if (Vs != Va) {
                        DiscardIFrameQueue();
                        Output(new DL_CONNECT_Indication(this, m_modulo));
                    }
                    T1.Stop();
                    T3.Start(T3V);
                    break;
                case AX25Frame_T.DISC:
                    DiscardIFrameQueue();
                    F = ((AX25_DISC)f).PF;
                    Output(new AX25_UA(F));
                    Output(new DL_DISCONNECT_Indication(this));
                    T1.Stop();
                    T3.Stop();
                    m_state = State_T.Disconnected;
                    break;
                case AX25Frame_T.UA:
                    Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorC));
                    EstablishDataLink();
                    ClearLayer3Initiated();
                    m_state = State_T.AwaitingConnect;
                    break;
                case AX25Frame_T.DM:
                    Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorE));
                    Output(new DL_DISCONNECT_Indication(this));
                    DiscardIQueue();
                    T1.Stop();
                    T3.Stop();
                    m_state = State_T.Disconnected;
                    break;
                case AX25Frame_T.FRMR:
                    Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorK));
                    EstablishDataLink();
                    ClearLayer3Initiated();
                    m_state = State_T.AwaitingConnect;
                    break;
                case AX25Frame_T.UI:
                    UI_Check((AX25_UI)f);
                    if (((AX25_UI)f).PF)
                        EnquiryResponse(true);
                    break;
                case AX25Frame_T.RR:
                    ClearPeerReceiverBusy();
                    CheckNeedForResponse();
                    if ((Va <= Nr) && (Nr <= Vs))
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
                    SetPeerReceiverBusy();
                    CheckNeedForResponse();
                    if ((Va <= Nr) && (Nr <= Vs))
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
                    ClearPeerReceiverBusy();
                    CheckNeedForResponse();
                    if ((Va <= Nr) && (Nr <= Vs))
                    {
                        if (((AX25_SREJ)f).PF)
                            Va = Nr;
                        T1.Stop();
                        T3.Start(T3V);
                        SelectT1Value();
                        PushOldIFrameNrOnQueue();
                    }
                    else
                    {
                        NrErrorRecovery();
                        m_state = State_T.AwaitingConnect;
                    }
                    break;
                case AX25Frame_T.REJ:
                    ClearPeerReceiverBusy();
                    CheckNeedForResponse();
                    if ((Va > Nr) || (Nr > Vs))
                    {
                        NrErrorRecovery();
                        m_state = State_T.AwaitingConnect;
                        break;
                    }
                    Va = Nr;
                    T1.Stop();
                    T3.Stop();
                    SelectT1Value();
                    InvokeRetransmission();
                    break;
                case AX25Frame_T.I:
                    if (!((AX25_I)f).P)
                    {
                        Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorS));
                        DiscardIFrame();
                        break;
                    }
                    if (((AX25_I)f).InfoFieldLength > N1)
                    {
                        Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorO));
                        ClearLayer3Initiated();
                        m_state = State_T.AwaitingConnect;
                        break;
                    }
                    if ((Va > Nr) || (Nr > Vs))
                    {
                        NrErrorRecovery();
                        m_state = State_T.AwaitingConnect;
                        break;
                    }
                    CheckIFrameAcknowledged();
                    if (OwnReceiverBusy)
                    {
                        DiscardContentsOfIFrame();
                        if (((AX25_I)f).P)
                        {
                            F = true;
                            Nr = Vr;
                            Output(new AX25_RNR(m_modulo, Nr, F));
                            ClearAcknowledgePending();
                        }
                        break;
                    }
                    if (Ns != Vr)
                    {
                        if (!RejectException)
                        {
                            if (SREJEnabled)
                            {
                                SaveContentsOfIFrame(((AX25_I)f).I);
                                if (SRejectException > 0)
                                {
                                    Nr = Ns;
                                    F = false;
                                }
                                else
                                {
                                    if (Ns > Vr + 1)
                                    {
                                        DiscardContentsOfIFrame();
                                        SetRejectException();
                                        F = (((AX25_I)f).P);
                                        Output(new AX25_REJ(m_modulo, Nr, F));
                                        ClearAcknowledgePending();
                                        break;
                                    }
                                    Nr = Vr;
                                    F = true;
                                    IncrementSRejectException();
                                    Output(new AX25_SREJ(m_modulo, Nr, F));
                                    ClearAcknowledgePending();
                                    break;
                                }
                            }
                            DiscardContentsOfIFrame();
                            SetRejectException();
                            F = ((AX25_I)f).P;
                            Nr = Vr;
                            Output(new AX25_REJ(m_modulo, Nr, F));
                            ClearAcknowledgePending();
                            break;
                        }
                        // RejectException
                        DiscardContentsOfIFrame();
                        if (!((AX25_I)f).P)
                            break;
                        F = true;
                        Nr = Vr;
                        Output(new AX25_RR(m_modulo, Nr, F));
                        ClearAcknowledgePending();
                    }
                    break;
                default:
                    break;
            } // end switch //
        }

        private void TimerRecovery(DataLinkPrimitive p)
        {
            switch (p.DataLinkPrimitiveType) {
                case DataLinkPrimitive_T.DL_CONNECT_Request_T:
                    Error();
                    break;
                case DataLinkPrimitive_T.DL_DISCONNECT_Request_T:
                    DiscardIFrameQueue();
                    RC = 0;
                    Output(new AX25_DISC(true));
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
                        SetOwnReceiverBusy();
                        Output(new AX25_RNR(m_modulo, Nr, false));
                        ClearAcknowledgePending();
                    }
                    break;
                case DataLinkPrimitive_T.DL_FLOW_ON_Request_T:
                    if (OwnReceiverBusy)
                    {
                        ClearOwnReceiverBusy();
                        Output(new AX25_RR(m_modulo, Nr, true));
                        ClearAcknowledgePending();
                        if (!T1.Running)
                        {
                            T3.Stop();
                            T1.Start(T1V);
                        }
                    }
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
                    m_version = Version_T.V2_0;
                    EstablishFromRecover(AX25Modulo.MOD8, ((AX25_SABM)f).PF);
                    break;
                case AX25Frame_T.SABME:
                    m_version = Version_T.V2_2;
                    EstablishFromRecover(AX25Modulo.MOD128, ((AX25_SABM)f).PF);
                    break;

                    
            } // end switch //
        }

        private void AwaitingConnect2_2(DataLinkPrimitive p)
        {
        }

        private void AwaitingConnect2_2(AX25Frame f)
        {
        }

        private void TimerT1Expiry()
        {
            switch (m_state)
            {
                case State_T.AwaitingConnect:
                    if (RC == N2)
                    {
                        DiscardFrameQueue();
                        Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorG));
                        Output(new DL_DISCONNECT_Indication(this));
                        m_state = State_T.Disconnected;
                    }
                    else
                    {
                        RC += 1;
                        Output(new AX25_SABM(true));
                        SelectT1Value();
                        T1.Start(T1V);
                    }
                    break;
                case State_T.AwaitingRelease:
                    if (RC == N2)
                    {
                        Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorH));
                        Output(new DL_DISCONNECT_Confirm(this));
                        m_state = State_T.Disconnected;
                    }
                    else
                    {
                        RC += 1;
                        Output(new AX25_DISC(true));
                        SelectT1Value();
                        T1.Start(T1V);
                    }
                    break;
                case State_T.Connected:
                    RC = 1;
                    TransmitEnquiry();
                    m_state = State_T.TimerRecovery;
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

        private void TransmitEnquiry()
        {
        }

        private void EnquiryResponse(bool f)
        {
        }

        private void UI_Command(byte[] data)
        {
        }

        private void I_Command(byte[] data)
        {
        }

        private void EstablishDataLink()
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
            Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorL));
            if ((m_state == State_T.Connected) || (m_state == State_T.TimerRecovery))
                Error();
        }

        private void InfoNotPermittedInFrame()
        {
            Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorM));
            if ((m_state == State_T.Connected) || (m_state == State_T.TimerRecovery))
                Error();
        }

        private void IncorrectUorSFrameLength()
        {
            Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorN));
            if ((m_state == State_T.Connected) || (m_state == State_T.TimerRecovery))
                Error();
        }

        private void Error()
        {
            DiscardIFrameQueue();
            EstablishDataLink();
            SetLayer3Initiated();
            m_state = State_T.AwaitingConnect;
        }

        private void UI_Check(AX25_UI f)
        {
        }

        private bool AbleToEstablish()
        {
            return true;
        }

        private void EstablishFromAwait(AX25Modulo modulo)
        {
            m_modulo = modulo;
            Output(new AX25_UA(F));
            ClearExceptionConditions();
            Vs = 0;
            Va = 0;
            Vr = 0;
            Output(new DL_CONNECT_Indication(this, modulo));
            SRT = m_config.SRT;
            T1V = 2 * SRT;
            T3.Start(T3V);
            m_state = State_T.Connected;
        }

        private void EstablishFromRecover(AX25Modulo modulo, bool p)
        {
            m_modulo = modulo;
            F = p;
            Output(new AX25_UA(F));
            ClearExceptionConditions();
            Output(new DL_ERROR_Indication(this, ErrorCode_T.ErrorF));
            if (Vs != Va)
            {
                DiscardIFrameQueue();
                Output(new DL_CONNECT_Indication(this, m_modulo));
            }
            T1.Stop();
            T3.Start(T3V);
            Vs = 0;
            Va = 0;
            Vr = 0;
            m_state = State_T.Connected;
        }

        private void ClearExceptionConditions()
        {
            PeerReceiverBusy = false;
            OwnReceiverBusy  = false;
            RejectException = false;
            SelectiveRejectException = false;
            AcknowledgePending = false;
        }

        private void DiscardQueue()
        {
        }

        private void DiscardIQueue()
        {
        }

        private void DiscardFrameQueue()
        {
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
                    if (PeerReceiverBusy)
                    {
                        PushIFrameOnQueue(data);
                        break;
                    }
                    if (Vs == Va + k)
                    {
                        PushIFrameOnQueue(data);
                        break;
                    }
                    Ns = Vs;
                    Nr = Vr;
                    p = 0;
                    I_Command(data);
                    Vs += 1;
                    AcknowledgePending = false;
                    if (!T1.Running)
                    {
                        T3.Stop();
                        T1.Start(T1V);
                    }
                    break;
                default:
                    break;
            } // end switch //
        }

        private void PushOldIFrameNrOnQueue()
        {
        }

        private void SelectT1Value()
        {
            T1V = T1.Elapsed;
        }

        private void SetOwnReceiverBusy()
        {
            OwnReceiverBusy = true;
        }

        private void ClearOwnReceiverBusy()
        {
            OwnReceiverBusy = true;
        }

        private void ClearAcknowledgePending()
        {
            AcknowledgePending = false;
        }

        private void SetPeerReceiverBusy()
        {
            PeerReceiverBusy = true;
        }

        private void ClearPeerReceiverBusy()
        {
            PeerReceiverBusy = false;
        }

        private void CheckNeedForResponse()
        {
        }

        private void CheckIFrameAcknowledged()
        {
        }

        private void NrErrorRecovery()
        {
        }

        private void InvokeRetransmission()
        {
        }

        private void DiscardIFrame()
        {
        }

        private void SaveContentsOfIFrame(byte[] data)
        {
        }

        private void DiscardContentsOfIFrame()
        {
        }

        private void SetRejectException()
        {
            RejectException = true;
            SRejectException = 0;
        }

        private void IncrementSRejectException()
        {
            SRejectException += 1;
        }

    }
}
