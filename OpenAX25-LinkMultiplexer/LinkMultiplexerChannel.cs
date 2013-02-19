//
// LinkMultiplexerChannel.cs
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
using System.Text.RegularExpressions;
using System.Threading;
using OpenAX25Contracts;
using OpenAX25Core;

/// <summary>
/// This class simulates a duplex physical state machine on top of a
/// L2Channel.
/// </summary>
namespace OpenAX25_LinkMultiplexer
{
    public sealed class LinkMultiplexerChannel :
        L3Channel, IL3DataLinkProvider
    {

        // States:
        private enum State_T
        {
            Idle = 0,
            SeizePending = 1,
            Seized = 2
        }

        private State_T m_state = State_T.Idle;
        private State_T State
        {
            get
            {
                return m_state;
            }
            set
            {
                if (value != m_state)
                {
                    m_state = value;
                    if (m_waitForSeizeFlag && (value != State_T.SeizePending))
                    {
                        m_waitForSeizeFlag = false;
                        lock (m_sync)
                        {
                            Monitor.PulseAll(m_sync);
                        }
                    }
                }
            }
        }

        // Queues:
        private struct LinkMultiplexerPrimitiveEntry
        {
            internal readonly ClientSession session;
            internal readonly LinkMultiplexerPrimitive primitive;
            internal readonly bool expedited;

            internal LinkMultiplexerPrimitiveEntry(
                ClientSession session, LinkMultiplexerPrimitive primitive, bool expedited)
            {
                this.session = session;
                this.primitive = primitive;
                this.expedited = expedited;
            }
        }

        private AX25Queue<LinkMultiplexerPrimitiveEntry> AwaitingQueue =
            new AX25Queue<LinkMultiplexerPrimitiveEntry>();
        private AX25Queue<LinkMultiplexerPrimitiveEntry> CurrentQueue =
            new AX25Queue<LinkMultiplexerPrimitiveEntry>();
        private AX25Queue<LinkMultiplexerPrimitiveEntry> ServedQueue =
            new AX25Queue<LinkMultiplexerPrimitiveEntry>();

        // Flags:
        private ISet<Guid> ServedList = new HashSet<Guid>();

        // Internal fields:
        private readonly IL3DataLinkProvider m_dataLinkProvider;
        private readonly Regex m_digipeat;

        private readonly IDictionary<L2Callsign, ClientEndpoint> m_clientEndpoints =
            new Dictionary<L2Callsign, ClientEndpoint>();

        private bool m_isOpen = false;
        private bool m_waitForSeizeFlag = false;
        private ILocalEndpoint m_physicalLayerEndpoint = null;
        private IL3Channel m_physicalLayerChannel = null;
        private ClientSession m_currentSession = ClientSession.NullSession;
        private Thread m_thread = null;
        private readonly Object m_sync = new Object();

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="properties">Properties of the channel.
		/// <list type="bullet">
		///   <listheader><term>Property name</term><description>Description</description></listheader>
		///   <item><term>Name</term><description>Name of the channel [Default: LMPX]</description></item>
        ///   <item><term>Target</term><description>Where to route packages to [Default: DPXPH]</description></item>
        ///   <item><term>Digi</term><description>Regular expression for digipeat [Default: "^$"]</description></item>
        /// </list>
		/// </param>
        public LinkMultiplexerChannel(IDictionary<string, string> properties)
            : base(properties)
		{
            string _val;
            if (!(m_target is IL3DataLinkProvider))
                throw new Exception("Target is not a datalink provider: " + m_target.Name);
            m_dataLinkProvider = (IL3DataLinkProvider)m_target;
            if (!m_properties.TryGetValue("Digi", out _val))
                _val = "^$";
            m_digipeat = new Regex(_val);
            m_runtime.RegisterChannel(this);
        }

        /// <summary>
        /// Open the interface.
        /// </summary>
        public override void Open()
        {
            lock (this)
            {
                base.Open();
                if (m_isOpen)
                    return;
                m_physicalLayerEndpoint = m_dataLinkProvider.RegisterLocalEndpoint(m_name, m_properties);
                if (m_physicalLayerEndpoint == null)
                    throw new Exception("Unable to register the local endpoint for physical layer");
                m_physicalLayerChannel = m_physicalLayerEndpoint.Bind(this, m_properties);
                if (m_physicalLayerChannel == null)
                    throw new Exception("Unable to register the channel for physical layer");
                m_isOpen = true;
                m_state = State_T.Idle;
                m_waitForSeizeFlag = false;
                m_thread = new Thread(new ThreadStart(Run2));
                m_thread.Start();
            } // end lock //
        }

        /// <summary>
        /// Close the interface.
        /// </summary>
        public override void Close()
        {
            lock (this)
            {
                if (!m_isOpen)
                    return;
                try
                {
                    if (m_physicalLayerEndpoint != null)
                    {
                        if (m_physicalLayerChannel != null)
                            m_physicalLayerEndpoint.Unbind(m_physicalLayerChannel);
                        m_dataLinkProvider.UnregisterLocalEndpoint(m_physicalLayerEndpoint);
                    }
                    if (m_thread != null)
                        m_thread.Abort();
                }
                catch (Exception e)
                {
                    m_runtime.Log(LogLevel.ERROR, m_name, "Error closing channel: " + e.Message);
                }
                finally
                {
                    m_physicalLayerChannel = null;
                    m_physicalLayerEndpoint = null;
                    m_isOpen = false;
                    m_thread = null;
                }
                base.Close();
            } // end lock //
        }

        /// <summary>
        /// Resets the channel. The data link is closed and reopened. All pending
        /// data is withdrawn.
        /// </summary>
        public override void Reset()
        {
            lock (this)
            {
                base.Reset();
                AwaitingQueue.Clear();
                CurrentQueue.Clear();
                ServedQueue.Clear();
                ServedList.Clear();
            } // end lock //
        }

        /// <summary>
        /// Receive a primitive from the physical layer.
        /// </summary>
        /// <param name="message">The primitive to send.</param>
        /// <param name="expedited">Send expedited if set.</param>
        protected override void Input(IPrimitive message, bool expedited)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            if (!(message is PhysicalLayerPrimitive))
                throw new Exception(String.Format("Invalid primitive of class {0}! Expected PhysicalLayerPrimitive.",
                    message.GetType().Name));
            PhysicalLayerPrimitive plp = (PhysicalLayerPrimitive)message;
            lock (this)
            {
                switch (State)
                {
                    case State_T.Idle: OnIdle(plp, expedited); break;
                    case State_T.Seized: OnSeized(plp, expedited); break;
                    case State_T.SeizePending: OnSeizePending(plp, expedited); break;
                } // end switch //
            }
        }

        /// <summary>
        /// Attach a new local endpoint.
        /// </summary>
        /// <param name="address">Local address of the endpoint.</param>
        /// <param name="properties">Endpoint properties, optional.</param>
        /// <returns>Local endopoint object that can be used to create sessions.</returns>
        public ILocalEndpoint RegisterLocalEndpoint(string address, IDictionary<string, string> properties)
        {
            L2Callsign callsign = new L2Callsign(address);
            lock (this)
            {
                // Normalize address:
                if (m_clientEndpoints.ContainsKey(callsign))
                    throw new DuplicateNameException(callsign.ToString());
                ClientEndpoint ep = new ClientEndpoint(this, callsign);
                m_clientEndpoints.Add(callsign, ep);
                return ep;
            } // end lock //
        }

        /// <summary>
        /// Unattach a local endpoint that where previously registered.
        /// </summary>
        /// <param name="endpoint">The endpoint to unregister.</param>
        public void UnregisterLocalEndpoint(ILocalEndpoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException("endpoint");
            if (!(endpoint is ClientEndpoint))
                throw new Exception(String.Format("Invalid class {0}. Expected {1}.",
                    endpoint.GetType().Name, typeof(ClientEndpoint).Name));
            ClientEndpoint ep = (ClientEndpoint)endpoint;
            L2Callsign key = ep.m_callsign;
            lock (this)
            {
                if (!m_clientEndpoints.ContainsKey(key))
                    throw new NotFoundException(key.ToString());
                m_clientEndpoints.Remove(key);
                ep.Close();
            } // end lock //
        }


        internal void Input(LinkMultiplexerPrimitive lmp, bool expedited, ClientSession clientSession)
        {
            if (lmp == null)
                throw new ArgumentNullException("lmp");
            if (clientSession == null)
                throw new ArgumentNullException("clientSession");
            lock (this)
            {
                switch (State)
                {
                    case State_T.Idle: OnIdle(lmp, expedited, clientSession); break;
                    case State_T.Seized: OnSeized(lmp, expedited, clientSession); break;
                    case State_T.SeizePending: OnSeizePending(lmp, expedited, clientSession); break;
                } // end switch //
            }
        }

        private void QueueEvent(LinkMultiplexerPrimitive lmp, bool expedited, ClientSession clientSession)
        {
            lock (this)
            {
                if ((m_currentSession != null) && (m_currentSession.m_id == clientSession.m_id))
                {
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "QU (Current):" + lmp.LinkMultiplexerPrimitiveTypeName);
                    CurrentQueue.Enqueue(new LinkMultiplexerPrimitiveEntry(clientSession, lmp, expedited));
                }
                else if (ServedList.Contains(clientSession.m_id))
                {
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "QU (Served):" + lmp.LinkMultiplexerPrimitiveTypeName);
                    ServedQueue.Enqueue(new LinkMultiplexerPrimitiveEntry(clientSession, lmp, expedited));
                }
                else
                {
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "QU (Awaiting):" + lmp.LinkMultiplexerPrimitiveTypeName);
                    AwaitingQueue.Enqueue(new LinkMultiplexerPrimitiveEntry(clientSession, lmp, expedited));
                }
            } // end lock //
            lock (m_sync)
            {
                Monitor.PulseAll(m_sync);
            } // end lock //
        }

        private void OnIdle(LinkMultiplexerPrimitive lmp, bool expedited, ClientSession session)
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, "RX (OnIdle) : " + lmp.LinkMultiplexerPrimitiveTypeName);
            switch (lmp.LinkMultiplexerPrimitiveType)
            {
                case LinkMultiplexerPrimitive_T.LM_DATA_Request_T :
                case LinkMultiplexerPrimitive_T.LM_SEIZE_Request_T :
                    m_currentSession = session;
                    AX25Queue<LinkMultiplexerPrimitiveEntry> q = new AX25Queue<LinkMultiplexerPrimitiveEntry>();
                    CurrentQueue.Enqueue(new LinkMultiplexerPrimitiveEntry(session, lmp, expedited));
                    foreach (LinkMultiplexerPrimitiveEntry e in AwaitingQueue)
                    {
                        if (e.session.m_id == session.m_id)
                            CurrentQueue.Enqueue(e);
                        else
                            q.Enqueue(e);
                    } // end foreach //
                    AwaitingQueue = q;
                    State = State_T.SeizePending;
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "TX (OnIdle) : PH_SEIZE_Request");
                    m_physicalLayerChannel.Send(new PH_SEIZE_Request());
                    break;
                case LinkMultiplexerPrimitive_T.LM_RELEASE_Request_T :
                    State = State_T.Idle;
                    break;
                case LinkMultiplexerPrimitive_T.LM_EXPEDITED_DATA_Request_T :
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "TX (OnIdle) : PH_EXPEDITED_DATA_Request");
                    m_physicalLayerChannel.Send(new PH_EXPEDITED_DATA_Request(
                        ((LM_EXPEDITED_DATA_Request)lmp).Frame));
                    State = State_T.Idle;
                    break;
                default :
                    QueueEvent(lmp, expedited, session);
                    State = State_T.Idle;
                    break;
            } // end switch //
        }

        private void OnSeizePending(LinkMultiplexerPrimitive lmp, bool expedited, ClientSession session)
        {
            if (m_currentSession == null)
                throw new ArgumentNullException("m_currentSession");
            if (lmp.LinkMultiplexerPrimitiveType == LinkMultiplexerPrimitive_T.LM_EXPEDITED_DATA_Request_T)
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (OnSeizePending) : PH_EXPEDITED_DATA_Request");
                m_physicalLayerChannel.Send(new PH_EXPEDITED_DATA_Request(
                    ((LM_EXPEDITED_DATA_Request)lmp).Frame));
                State = State_T.SeizePending;
                return;
            }
            if (m_waitForSeizeFlag || (session.m_id != m_currentSession.m_id))
            {
                QueueEvent(lmp, expedited, session);
                State = State_T.SeizePending;
                return;
            }
            switch (lmp.LinkMultiplexerPrimitiveType)
            {
                case LinkMultiplexerPrimitive_T.LM_DATA_Request_T :
                    CurrentQueue.PutBack(new LinkMultiplexerPrimitiveEntry(session, lmp, expedited));
                    m_waitForSeizeFlag = true;
                    State = State_T.SeizePending;
                    break;
                case LinkMultiplexerPrimitive_T.LM_SEIZE_Request_T :
                    State = State_T.SeizePending;
                    break;
                case LinkMultiplexerPrimitive_T.LM_RELEASE_Request_T :
                    FinishCurrentTransmission();
                    State = State_T.Idle;
                    break;
                default :
                    QueueEvent(lmp, expedited, session);
                    State = State_T.SeizePending;
                    break;
            } // end case //
        }

        private void OnSeized(LinkMultiplexerPrimitive lmp, bool expedited, ClientSession session)
        {
            if (m_currentSession == null)
                throw new ArgumentNullException("m_currentSession");
            if (lmp.LinkMultiplexerPrimitiveType == LinkMultiplexerPrimitive_T.LM_EXPEDITED_DATA_Request_T)
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (OnSeized) : PH_EXPEDITED_DATA_Request");
                m_physicalLayerChannel.Send(new PH_EXPEDITED_DATA_Request(
                    ((LM_EXPEDITED_DATA_Request)lmp).Frame));
                State = State_T.Seized;
                return;
            }
            if (session.m_id != m_currentSession.m_id)
            {
                QueueEvent(lmp, expedited, session);
                State = State_T.Seized;
                return;
            }
            switch (lmp.LinkMultiplexerPrimitiveType)
            {
                case LinkMultiplexerPrimitive_T.LM_DATA_Request_T :
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "TX (OnSeized) : PH_DATA_Request");
                    m_physicalLayerChannel.Send(new PH_DATA_Request(((LM_DATA_Request)lmp).Frame));
                    State = State_T.Seized;
                    break;
                case LinkMultiplexerPrimitive_T.LM_SEIZE_Request_T :
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "TX (OnSeized) : LM_SEIZE_Confirm");
                    session.SendToPeer(new LM_SEIZE_Confirm(), expedited);
                    State = State_T.Seized;
                    break;
                case LinkMultiplexerPrimitive_T.LM_RELEASE_Request_T :
                    FinishCurrentTransmission();
                    State = State_T.Idle;
                    break;
                default :
                    QueueEvent(lmp, expedited, session);
                    State = State_T.Seized;
                    break;
            } // end switch //
        }

        private void OnSeizePending(PhysicalLayerPrimitive plp, bool expedited)
        {
            if (m_currentSession == null)
                throw new ArgumentNullException("m_currentSession");
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, "RX (OnSeizePending) : " + plp.PhysicalLayerPrimitiveTypeName);
            switch (plp.PhysicalLayerPrimitiveType)
            {
                case PhysicalLayerPrimitive_T.PH_SEIZE_Confirm_T:
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "TX (OnSeizePending) : LM_SEIZE_Confirm");
                    m_currentSession.SendToPeer(new LM_SEIZE_Confirm(), expedited);
                    State = State_T.Seized;
                    break;
                case PhysicalLayerPrimitive_T.PH_DATA_Indication_T:
                    FrameReceived((PH_DATA_Indication)plp, expedited);
                    State = State_T.SeizePending;
                    break;
                case PhysicalLayerPrimitive_T.PH_BUSY_Indication_T:
                    SendToAllSessions(plp, expedited);
                    State = State_T.SeizePending;
                    break;
                case PhysicalLayerPrimitive_T.PH_QUIET_Indication_T:
                    SendToAllSessions(plp, expedited);
                    State = State_T.SeizePending;
                    break;
                default:
                    break;
            } // end switch //
        }

        private void OnSeized(PhysicalLayerPrimitive plp, bool expedited)
        {
            if (m_currentSession == null)
                throw new ArgumentNullException("m_currentSession");
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, "RX (OnSeized) : " + plp.PhysicalLayerPrimitiveTypeName);
            switch (plp.PhysicalLayerPrimitiveType)
            {
                case PhysicalLayerPrimitive_T.PH_SEIZE_Confirm_T:
                    State = State_T.Seized;
                    break;
                case PhysicalLayerPrimitive_T.PH_DATA_Indication_T:
                    FrameReceived((PH_DATA_Indication)plp, expedited);
                    State = State_T.Seized;
                    break;
                case PhysicalLayerPrimitive_T.PH_BUSY_Indication_T:
                    SendToAllSessions(plp, expedited);
                    State = State_T.Seized;
                    break;
                case PhysicalLayerPrimitive_T.PH_QUIET_Indication_T:
                    SendToAllSessions(plp, expedited);
                    State = State_T.Seized;
                    break;
                default:
                    break;
            } // end switch //
        }

        private void OnIdle(PhysicalLayerPrimitive plp, bool expedited)
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, "RX (OnIdle) : " + plp.PhysicalLayerPrimitiveTypeName);
            switch (plp.PhysicalLayerPrimitiveType)
            {
                case PhysicalLayerPrimitive_T.PH_SEIZE_Confirm_T :
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "TX (OnIdle) : PH_RELEASE_Request");
                    m_physicalLayerChannel.Send(new PH_RELEASE_Request());
                    State = State_T.Idle;
                    break;
                case PhysicalLayerPrimitive_T.PH_DATA_Indication_T :
                    FrameReceived((PH_DATA_Indication)plp, expedited);
                    State = State_T.Idle;
                    break;
                case PhysicalLayerPrimitive_T.PH_BUSY_Indication_T:
                    SendToAllSessions(plp, expedited);
                    State = State_T.Idle;
                    break;
                case PhysicalLayerPrimitive_T.PH_QUIET_Indication_T:
                    SendToAllSessions(plp, expedited);
                    State = State_T.Idle;
                    break;
                default :
                    break;
            } // end switch //
        }

        private void SendToAllSessions(IPrimitive message, bool expedited)
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, "TX (SendToAllSessions) : " + message.GetType().Name);
            foreach (ClientEndpoint ce in m_clientEndpoints.Values)
            {
                foreach (ClientSession cs in ce.m_sessions.Values)
                    try
                    {
                        cs.SendToPeer(message, expedited);
                    }
                    catch{ }
            } // end foreach //
        }

        private void FinishCurrentTransmission()
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name,
                    "TX (FinishCurrentTransmission) : PH_RELEASE_Request");
            m_physicalLayerChannel.Send(new PH_RELEASE_Request());
            while (CurrentQueue.Count > 0)
                ServedQueue.Enqueue(CurrentQueue.Dequeue());
            ServedList.Add(m_currentSession.m_id);
            m_currentSession = ClientSession.NullSession;
        }

        private void FrameReceived(PH_DATA_Indication di, bool expedited)
        {
            // FCS OK? -- Already assumed.
            AX25Header header = di.Frame.Header;
            if (header.MustDigi())
            {
                string nextHop = header.NextHop.ToString();
                if (!m_digipeat.IsMatch(nextHop.ToString()))
                {
                    m_runtime.Log(LogLevel.INFO, m_name, "Drop non digipeating frame to " + nextHop);
                    return;
                }
                header.Digipeat();
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX (Digipeat) : PH_EXPEDITED_DATA_Request");
                m_physicalLayerChannel.Send(new PH_EXPEDITED_DATA_Request(di.Frame), true);
                return;
            }
            // Ok, no digi. Check if we have the destination:
            L2Callsign destination = header.Destination;
            ClientEndpoint ep;
            if (!m_clientEndpoints.TryGetValue(destination, out ep))
            {
                m_runtime.Log(LogLevel.INFO, m_name, "Drop unknown destination frame to " + destination);
                return;
            }
            // Ok, we have a endpoint, check if there is a session for this path:
            ClientSession session = ep.GetSession(header);
            if (session == null)
            {
                m_runtime.Log(LogLevel.INFO, m_name, "Drop unknown path frame to " + destination);
                return;
            }
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, "TX (Data) : LM_DATA_Indication");
            session.SendToPeer(new LM_DATA_Indication(di.Frame), false);
        }

        /// <summary>
        /// Thread routine.
        /// </summary>
        private void Run2()
        {
            m_runtime.Log(LogLevel.INFO, m_name, "Queue thread started");
            while (true)
            {
                try
                {
                    lock (this)
                    {
                        switch (State) {
                            case State_T.Idle :
                                if (AwaitingQueue.Count == 0)
                                {
                                    AX25Queue<LinkMultiplexerPrimitiveEntry>.Move(ServedQueue, AwaitingQueue);
                                    ServedList.Clear();
                                    if (AwaitingQueue.Count == 0)
                                        goto L_WAIT;
                                }
                                {
                                    LinkMultiplexerPrimitiveEntry e = AwaitingQueue.Dequeue();
                                    OnIdle(e.primitive, e.expedited, e.session);
                                }
                                continue;
                            case State_T.SeizePending :
                                if (m_waitForSeizeFlag)
                                    goto L_WAIT;
                                if (CurrentQueue.Count == 0)
                                    goto L_WAIT;
                                {
                                    LinkMultiplexerPrimitiveEntry e = CurrentQueue.Dequeue();
                                    OnSeizePending(e.primitive, e.expedited, e.session);
                                }
                                continue;
                            case State_T.Seized :
                                if (CurrentQueue.Count == 0)
                                {
                                    FinishCurrentTransmission();
                                    State = State_T.Idle;
                                }
                                else
                                {
                                    LinkMultiplexerPrimitiveEntry e = CurrentQueue.Dequeue();
                                    OnSeized(e.primitive, e.expedited, e.session);
                                }
                                continue;
                        } // end switch //
                    } // end lock //
                L_WAIT:
                    lock (m_sync)
                    {
                        Monitor.Wait(m_sync);
                    }
                }
                catch (Exception e)
                {
                    m_runtime.Log(LogLevel.ERROR, m_name, "Exception in queue thread: " + e.Message);
                }
            } // end while //
        }
    }
}
