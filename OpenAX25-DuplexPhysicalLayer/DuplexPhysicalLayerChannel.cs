//
// DuplexPhysicalLayerChannel.cs
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
using System.Threading;
using OpenAX25Contracts;
using OpenAX25Core;
using System.Collections.Concurrent;

namespace OpenAX25_DuplexPhysicalLayer
{
    /// <summary>
    /// This class simulates a duplex physical state machine on top of a
    /// L2Channel.
    /// </summary>
    public sealed class DuplexPhysicalLayerChannel :
        L2Channel, IL3Channel, IL3DataLinkProvider, ILocalEndpoint
    {

        // Timers:
        private readonly AX25Timer T091;
        private readonly AX25Timer T105;
        private readonly AX25Timer T107;

        // Flags and Variables:
        private readonly AX25Modulo Modulo;
        private bool Interrupted = false;

        // Values:
        private readonly long T091_V;
        private readonly long T105_V;
        private readonly long T107_V;

        // States:
        private enum ReceiverState_T
        {
            ReceiverReady = 0,
            Receiving = 1
        }

        private ReceiverState_T ReceiverState =
            ReceiverState_T.ReceiverReady;

        private enum TransmitterState_T
        {
            TransmitterReady = 0,
            TransmitterStart = 1,
            Transmitting = 2
        }

        private TransmitterState_T TransmitterState =
            TransmitterState_T.TransmitterReady;

        // Queues:
        private struct NormalQueueEntry
        {
            internal readonly PhysicalLayerPrimitive plp;
            internal readonly bool expedited;

            internal NormalQueueEntry(PhysicalLayerPrimitive _plp, bool _expedited)
            {
                plp = _plp;
                expedited = _expedited;
            }
        };
        Queue<NormalQueueEntry> NormalQueue = new Queue<NormalQueueEntry>();

        // Internal:
        private long T091seq = -1;
        private long T105seq = -1;
        private long T107seq = -1;
        private IL3Channel UpperEndpoint = null;
        private IDictionary<string, string> EndpointProperties = null;
        private IDictionary<string, string> EndpointPropertiesBackup = null;
        private Guid EndpointId = Guid.Empty;
        private string EndpointAddress = String.Empty;
        private string EndpointAddressBackup = String.Empty;
        private bool IsOpen = false;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="properties">Properties of the channel.
		/// <list type="bullet">
		///   <listheader><term>Property name</term><description>Description</description></listheader>
		///   <item><term>Name</term><description>Name of the channel [Mandatory]</description></item>
		///   <item><term>Target</term><description>Where to route packages to [Default: ROUTER]</description></item>
        ///   <item><term>Modulo</term><description>AX25 modulo to use (8|128) [Default: 16]</description></item>
        ///   <item><term>T091</term><description>Squelch timer [ms] [Default: 500]</description></item>
        ///   <item><term>T105</term><description>Remote receiver sync [ms] [Default: 500]</description></item>
        ///   <item><term>T107</term><description>Anti hogging limit [ms] [Default: 60000]</description></item>
        /// </list>
		/// </param>
        public DuplexPhysicalLayerChannel(IDictionary<string, string> properties)
			: base(properties)
		{
            string _val;

            if (!properties.TryGetValue("Modulo", out _val))
                _val = "8";
            if ("8".Equals(_val))
                Modulo = AX25Modulo.MOD8;
            else if ("128".Equals(_val))
                Modulo = AX25Modulo.MOD128;
            else
                throw new ArgumentOutOfRangeException(
                    String.Format(
                        "Modulo: Value: \"{0}\", Problem: (Only \"8\" and \"128\" permitted)",
                        _val));

            if (!properties.TryGetValue("T091", out _val))
                _val = "500";
            try
            {
                T091_V = Int64.Parse(_val);
                if (T091_V < 0)
                    throw new ArgumentOutOfRangeException("0 .. MAX_INT");
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException(
                    String.Format("T091: Value: \"{0}\", Problem: {1}", _val, ex.Message));
            }

            if (!properties.TryGetValue("T105", out _val))
				_val = "500";
            try
            {
                T105_V = Int64.Parse(_val);
                if (T105_V < 0)
                    throw new ArgumentOutOfRangeException("0 .. MAX_INT");
            }
			catch (Exception ex) {
				throw new InvalidPropertyException(
                    String.Format("T105: Value: \"{0}\", Problem: {1}", _val, ex.Message));
			}

            if (!properties.TryGetValue("T107", out _val))
                _val = "60000";
            try
            {
                T107_V = Int64.Parse(_val);
                if (T107_V < 0)
                    throw new ArgumentOutOfRangeException("0 .. MAX_INT");
            }
            catch (Exception ex)
            {
                throw new InvalidPropertyException(
                    String.Format("T107: Value: \"{0}\", Problem: {1}", _val, ex.Message));
            }

            T091 = new AX25Timer(this, new TimerCallback(OnT091Callback));
            T105 = new AX25Timer(this, new TimerCallback(OnT105Callback));
            T107 = new AX25Timer(this, new TimerCallback(OnT107Callback));
        }

        /// <summary>
        /// Reset the channel and withdraw all frames in the
        /// receiver and transmitter queue.
        /// </summary>
        public override void Reset()
        {
            lock (this)
            {
                base.Reset();
                while (m_queue.Count > 0)
                    m_queue.Take();
            }
        }

        /// <summary>
        /// Open the channel, so that data actually be transmitted and received.
        /// </summary>
        public override void Open()
        {
            lock (this)
            {
                if (IsOpen)
                    return;
                ReceiverState = ReceiverState_T.ReceiverReady;
                TransmitterState = TransmitterState_T.TransmitterReady;
                IsOpen = true;
                base.Open();
                if (m_thread != null) // Already open.
                    return;
                m_thread = new Thread(new ThreadStart(Run));
                m_thread.Start();
            }
        }

        /// <summary>
        /// Close the channel. No data will be transmitted or received. All queued
        /// data is preserved.
        /// </summary>
        public override void Close()
        {
            lock (this)
            {
                if (!IsOpen)
                    return;
                base.Close();
                if (ReceiverState == ReceiverState_T.Receiving)
                {
                    if (m_runtime.LogLevel >= LogLevel.DEBUG)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "TX PH_QUIET_Indication");
                    if (UpperEndpoint != null)
                        UpperEndpoint.Send(new PH_QUIET_Indication());
                    ReceiverState = ReceiverState_T.ReceiverReady;
                }
                TransmitterState = TransmitterState_T.TransmitterReady;
                T091seq = T091.Stop();
                T105seq = T105.Stop();
                T107seq = T107.Stop();
                Interrupted = true;
                IsOpen = false;
                if (m_thread == null) // Already closed.
                    return;
                m_thread.Abort();
                m_thread = null;
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
            if (EndpointProperties != null)
                throw new Exception("Unable to provide more than one endpoint on Physical Layer");
            lock (this)
            {
                EndpointAddressBackup = EndpointAddress;
                EndpointId = Guid.NewGuid();
                EndpointAddress = (!String.IsNullOrEmpty(address)) ? address : EndpointId.ToString();
                if (properties != null)
                    EndpointProperties = new Dictionary<string, string>(properties);
                else
                    EndpointProperties = new Dictionary<string, string>();
                EndpointPropertiesBackup = EndpointProperties;
                UpperEndpoint = null;
                return this;
            }
        }

        /// <summary>
        /// Unattach a local endpoint that where previously registered.
        /// </summary>
        /// <param name="endpoint">The endpoint to unregister.</param>
        public void UnregisterLocalEndpoint(ILocalEndpoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException("endpoint");
            if (endpoint.GetType() != this.GetType())
                throw new Exception("Invalid type " + endpoint.GetType().Name);
            if (endpoint.GetHashCode() != this.GetHashCode())
                throw new Exception("Invalid instance");
            lock (this)
            {
                EndpointProperties = null;
                EndpointPropertiesBackup = null;
                EndpointAddress = String.Empty;
                EndpointAddressBackup = String.Empty;
                UpperEndpoint = null;
            }
        }

        /// <summary>
        /// Get unique ID of the endpoint.
        /// </summary>
        public Guid Id { get { return EndpointId; }  }

        /// <summary>
        /// Get the address of this endpoint.
        /// </summary>
        public string Address { get { return EndpointAddress; } }

        /// <summary>
        /// Attach a new session.
        /// </summary>
        /// <param name="receiver">Receiver channel.</param>
        /// <param name="properties">Properties of the channel.
        /// <list type="bullet">
        ///   <listheader><term>Property name</term><description>Description</description></listheader>
        ///   <item><term>Name</term><description>Name of the channel [Mandatory]</description></item>
        /// </list>
        /// </param>
        /// <param name="alias">Name alias for better tracing [Default: Value of "Name"]</param>
        /// <returns>Transmitter channel</returns>
        public IL3Channel Bind(IL3Channel receiver, IDictionary<string, string> properties, string alias = null)
        {
            if (receiver == null)
                throw new ArgumentNullException("receiver");
            lock (this)
            {
                if (UpperEndpoint != null)
                    throw new Exception("Unable to provide more than one session on Physical Layer");
                UpperEndpoint = receiver;
                if (properties != null)
                    EndpointProperties = new Dictionary<string, string>(properties);
                if (!String.IsNullOrEmpty(alias))
                    EndpointAddress = alias;
                return this;
            }
        }

        /// <summary>
        /// Unattach a session that where previously registered.
        /// </summary>
        /// <param name="transmitter">
        /// Tranmitter returned from previous call to Bind.
        /// </param>
        public void Unbind(IL3Channel transmitter)
        {
            if (transmitter == null)
                throw new ArgumentNullException("transmitter");
            if (transmitter.GetType() != this.GetType())
                throw new Exception("Invalid type " + transmitter.GetType().Name);
            if (transmitter.GetHashCode() != this.GetHashCode())
                throw new Exception("Invalid instance");
            lock (this)
            {
                UpperEndpoint = null;
                EndpointAddress = EndpointAddressBackup;
                EndpointProperties = EndpointPropertiesBackup;
            }
        }

        /// <summary>
        /// Get or set the target channel.
        /// </summary>
        public IL3Channel L3Target {
            get
            {
                return L3NullChannel.Instance;
            }
        }
        
        /// <summary>
		/// Actually write data to the higher level.
		/// </summary>
		/// <param name="frame">Data to send</param>
        protected override void OnForward(L2Frame frame)
        {
            lock (this)
            {
                if (!IsOpen)
                {
                    m_runtime.Log(LogLevel.WARNING, m_name, "OnForward (Channel closed) :"
                        + frame.ToString());
                    return;
                }
                switch (ReceiverState)
                {
                    case ReceiverState_T.ReceiverReady:
                        OnReceiverReady(frame);
                        break;
                    case ReceiverState_T.Receiving:
                        OnReceiving(frame);
                        break;
                } // end switch //
            } // end lock //
        }

        private void OnReceiving(L2Frame frame)
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
            {
                string text = String.Format(
                    "RX (OnReceiving) : {0} NO={1}",
                    HexConverter.ToHexString(frame.data, true), frame.no);
                m_runtime.Log(LogLevel.DEBUG, m_name, text);
            }

            T091seq = T091.Stop();
            IL3Channel ep = UpperEndpoint; // Save pointer for later access
            if (ep != null)
            {
                AX25Frame f = new AX25Frame(frame.data, Modulo);
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX PH_DATA_Indication: "
                        + f.ToString());
                PH_DATA_Indication di = new PH_DATA_Indication(f);
                ep.Send(di);
            }
            else
            {
                m_runtime.Log(LogLevel.ERROR, m_name, "!!! UpperEndpoint is null");
            }
            T091seq = T091.Start(T091_V);

            ReceiverState = ReceiverState_T.Receiving;
        }

        private void OnReceiverReady(L2Frame frame)
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
            {
                string text = String.Format(
                    "RX (OnReceiverReady : ({0}) NO={1}",
                    HexConverter.ToHexString(frame.data, true), frame.no);
                m_runtime.Log(LogLevel.DEBUG, m_name, text);
            }

            // Aquisition of signal
            T091seq = T091.Stop();
            T105seq = T105.Stop();
            T107seq = T107.Stop();
            IL3Channel ep = UpperEndpoint; // Save pointer for later access
            if (ep != null)
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX PH_BUSY_Indication");
                ep.Send(new PH_BUSY_Indication());
            }
            else
            {
                m_runtime.Log(LogLevel.ERROR, m_name, "!!! UpperEndpoint is null");
            }
            ReceiverState = ReceiverState_T.Receiving;
            // Chaining
            OnReceiving(frame);
        }

        /// <summary>
        /// Send a Data Link primitive to the serving object.
        /// </summary>
        /// <param name="p">Data Link primitive to send</param>
        /// <param name="expedited">Send expedited if set.</param>
        public void Send(IPrimitive p, bool expedited)
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, "ARRIVED(1): " + p.GetType().Name);
            lock (this)
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "ARRIVED(2): " + p.GetType().Name);
                m_queue.Add(new Entry(p, expedited));
            }
        }

        private void SynchronizedSend(IPrimitive p, bool expedited)
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, "PROCESSING(1): " + p.GetType().Name);
            lock (this)
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "PROCESSING(2): " + p.GetType().Name);
                if (!IsOpen)
                {
                    m_runtime.Log(LogLevel.WARNING, m_name, "RX: DATA DROP (Channel not open) : "
                        + p.GetType().Name);
                    return;
                }
                switch (TransmitterState)
                {
                    case TransmitterState_T.TransmitterReady:
                        OnTransmitterReady(p, expedited);
                        break;
                    case TransmitterState_T.TransmitterStart:
                        OnTransmitterStart(p, expedited);
                        break;
                    case TransmitterState_T.Transmitting:
                        OnTransmitting(p, expedited);
                        break;
                } // end switch //
            } // end lock //
        }

        private void OnTransmitterReady(IPrimitive p, bool expedited)
        {
            if (p is PhysicalLayerPrimitive)
            {
                PhysicalLayerPrimitive plp = (PhysicalLayerPrimitive)p;
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "OnTransmitterReady " + plp.PhysicalLayerPrimitiveTypeName);
                switch (plp.PhysicalLayerPrimitiveType)
                {
                    case PhysicalLayerPrimitive_T.PH_SEIZE_Request_T :
                        T105seq = T105.Start(T105_V);
                        // Turn on transmitter
    		            m_runtime.Log(LogLevel.INFO, m_name, "Turn on transmitter");
                        TransmitterState = TransmitterState_T.TransmitterStart;
                        break;
                    case PhysicalLayerPrimitive_T.PH_RELEASE_Request_T :
                        TransmitterState = TransmitterState_T.TransmitterReady;
                        break;
                    default :
                        NormalQueue.Enqueue(new NormalQueueEntry(plp, expedited));
                        TransmitterState = TransmitterState_T.TransmitterReady;
                        break;
                } // end switch //
            }
            else
            {
                m_runtime.Log(LogLevel.WARNING, m_name,
                    String.Format(
                        "Received unexpected primitive of type {0}. Expected PhysicalLayerPrimitive.",
                        p.GetType().Name));
            }
        }

        private void OnTransmitterStart(IPrimitive p, bool expedited)
        {
            if (p is PhysicalLayerPrimitive)
            {
                PhysicalLayerPrimitive plp = (PhysicalLayerPrimitive)p;
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "OnTransmitterStart " + plp.PhysicalLayerPrimitiveTypeName);
                NormalQueue.Enqueue(new NormalQueueEntry(plp, expedited));
                TransmitterState = TransmitterState_T.TransmitterStart;
            }
            else
            {
                m_runtime.Log(LogLevel.WARNING, m_name,
                    String.Format(
                        "Received unexpected primitive of type {0}. Expected PhysicalLayerPrimitive.",
                        p.GetType().Name));
            }
        }

        private void OnTransmitting(IPrimitive p, bool expedited)
        {
            if (p is PhysicalLayerPrimitive)
            {
                PhysicalLayerPrimitive plp = (PhysicalLayerPrimitive)p;
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "OnTransmitting " + plp.PhysicalLayerPrimitiveTypeName);
                switch (plp.PhysicalLayerPrimitiveType)
                {
                    case PhysicalLayerPrimitive_T.PH_SEIZE_Request_T:
                        if (m_runtime.LogLevel >= LogLevel.DEBUG)
                            m_runtime.Log(LogLevel.DEBUG, m_name, "TX PH_SEIZE_Confirm");
                        if (UpperEndpoint != null)
                            UpperEndpoint.Send(new PH_SEIZE_Confirm());
                        TransmitterState = TransmitterState_T.Transmitting;
                        break;
                    case PhysicalLayerPrimitive_T.PH_RELEASE_Request_T:
                        Interrupted = false;
                        T107seq = T107.Stop();
                        // Turn off transmitter
                        m_runtime.Log(LogLevel.INFO, m_name, "Turn off transmitter");
                        TransmitterState = TransmitterState_T.TransmitterReady;
                        break;
                    case PhysicalLayerPrimitive_T.PH_DATA_Request_T:
                        SendFrame(((PH_DATA_Request)plp).Frame, false);
                        TransmitterState = TransmitterState_T.Transmitting;
                        break;
                    case PhysicalLayerPrimitive_T.PH_EXPEDITED_DATA_Request_T:
                        SendFrame(((PH_DATA_Request)plp).Frame, true);
                        TransmitterState = TransmitterState_T.Transmitting;
                        break;
                    default:
                        NormalQueue.Enqueue(new NormalQueueEntry(plp, expedited));
                        TransmitterState = TransmitterState_T.Transmitting;
                        break;
                } // end switch //
            }
            else
            {
                m_runtime.Log(LogLevel.WARNING, m_name,
                    String.Format(
                        "Received unexpected primitive of type {0}. Expected PhysicalLayerPrimitive.",
                        p.GetType().Name));
            }
        }

        private void SendFrame(AX25Frame frame, bool expedited)
        {
            if (m_target != null)
            {
                L2Frame _frame = new L2Frame(
                    m_runtime.NewFrameNo(), expedited, frame.Octets, EndpointProperties);
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX frame : " + _frame.ToString());
                m_target.ForwardFrame(_frame);
            }
        }

        private void OnT091Callback()
        {
            lock (this)
            {
                if (T091.SequenceNumber != T091seq)
                    return;
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "OnT091Callback");
                switch (ReceiverState)
                {
                    case ReceiverState_T.Receiving :
        	            if (m_runtime.LogLevel >= LogLevel.DEBUG)
				            m_runtime.Log(LogLevel.DEBUG, m_name, "TX PH_QUIET_Indication");
                        if (UpperEndpoint != null)
                            UpperEndpoint.Send(new PH_QUIET_Indication());
                        ReceiverState = ReceiverState_T.ReceiverReady;
                        break;
                    default :
                        break;
                } // end switch //
            } // end lock //
        }

        private void OnT105Callback()
        {
            lock (this)
            {
                if (T105.SequenceNumber != T105seq)
                    return;
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "OnT105Callback");
                switch (TransmitterState)
                {
                    case TransmitterState_T.TransmitterStart :
                        if (!Interrupted)
                        {
                            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                                m_runtime.Log(LogLevel.DEBUG, m_name, "TX PH_SEIZE_Confirm");
                            if (UpperEndpoint != null)
                                UpperEndpoint.Send(new PH_SEIZE_Confirm());
                        }
                        TransmitterState = TransmitterState_T.Transmitting;
                        Queue<NormalQueueEntry> q = NormalQueue;
                        NormalQueue = new Queue<NormalQueueEntry>();
                        T107seq = T107.Start(T107_V);
                        foreach (NormalQueueEntry e in q)
                            Send(e.plp, e.expedited);
                        break;
                    default :
                        break;
                } // end switch //
            }
        }

        private void OnT107Callback()
        {
            lock (this)
            {
                if (T107.SequenceNumber != T107seq)
                    return;
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "OnT107Callback");
                switch (TransmitterState)
                {
                    case TransmitterState_T.Transmitting:
                        Interrupted = true;
                        // Turn off transmitter
                        if (m_runtime.LogLevel >= LogLevel.INFO)
				            m_runtime.Log(LogLevel.INFO, m_name, "Turn off transmitter");
                        TransmitterState = TransmitterState_T.TransmitterReady;
                        break;
                    default:
                        break;
                } // end switch //
            }
        }

        private static void OnT091Callback(object userData)
        {
            ((DuplexPhysicalLayerChannel)userData).OnT091Callback();
        }

        private static void OnT105Callback(object userData)
        {
            ((DuplexPhysicalLayerChannel)userData).OnT105Callback();
        }

        private static void OnT107Callback(object userData)
        {
            ((DuplexPhysicalLayerChannel)userData).OnT107Callback();
        }

        private void Run()
        {
            m_runtime.Log(LogLevel.INFO, m_name, "Consumer thread started");
            while (true)
            {
                try
                {
                    Entry e = m_queue.Take();
                    SynchronizedSend(e.p, e.expedited);
                }
                catch (Exception e)
                {
                    m_runtime.Log(LogLevel.ERROR, m_name, "Exception in consumer thread: " + e.Message);
                }
            }
        }

        private struct Entry
        {
            internal readonly IPrimitive p;
            internal readonly bool expedited;

            internal Entry(IPrimitive _p, bool _expedited)
            {
                p = _p;
                expedited = _expedited;
            }
        }

        private BlockingCollection<Entry> m_queue = new BlockingCollection<Entry>(
            new ConcurrentQueue<Entry>());
        private Thread m_thread = null;

    }
}
