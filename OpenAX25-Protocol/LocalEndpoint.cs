using System;
using OpenAX25Contracts;
using OpenAX25Core;
using System.Collections.Generic;

namespace OpenAX25_Protocol
{
    internal class LocalEndpoint : ILocalEndpoint, IDisposable
    {
        internal readonly Guid m_id;
        internal readonly string m_name;
        internal readonly L2Callsign m_localCallsign;
        internal readonly string m_key;
        internal readonly ProtocolChannel m_protoChannel;

        private readonly IDictionary<Guid, Session> m_sessions = new Dictionary<Guid, Session>();
        private readonly Runtime m_runtime = Runtime.Instance;

        internal LocalEndpoint(ProtocolChannel channel, L2Callsign callsign, string key,
            IDictionary<string,string> properties = null)
        {
            m_id = Guid.NewGuid();
            m_name = channel.Name + ":" + key;
            m_localCallsign = callsign;
            m_key = key;
            m_protoChannel = channel;
        }

        /// <summary>
        /// Return the endpoint address.
        /// </summary>
        public string Address
        {
            get
            {
                return m_key;
            }
        }

        /// <summary>
        /// Return the endpoint ID.
        /// </summary>
        public Guid Id
        {
            get
            {
                return m_id;
            }
        }

        /// <summary>
        /// Attach a new session.
        /// </summary>
        /// <param name="receiver">Receiver channel.</param>
        /// <param name="alias">Name alias for better tracing [Default: Value of "Name"]</param>
        /// <returns>Transmitter channel.</returns>
        public IL3Channel Bind(IL3Channel receiver, IDictionary<string, string> properties, string alias = null)
        {
            Session session = new Session(this, receiver, properties, alias);
            m_runtime.Log(LogLevel.INFO, m_name, "Bind " + receiver.Name + " to " + session.Name);
            lock (m_sessions)
            {
                m_sessions.Add(session.m_id, session);
            }
            session.Open();
            return session;
        }

        /// <summary>
        /// Unattach a session that where previously registered.
        /// </summary>
        /// <param name="session">
        /// Tranmitter returned from previous call to Bind.
        /// </param>
        public void Unbind(IL3Channel session)
        {
            if (!(session is Session))
                throw new ArgumentException("Invalid session");
            m_runtime.Log(LogLevel.INFO, m_name, "Unind " + session.Name);
            lock (m_sessions)
            {
                Session _session = m_sessions[((Session)session).m_id];
                if (_session == null)
                    throw new ArgumentException("Transmitter not registered: " + _session.m_id);
                m_sessions.Remove(_session.m_id);
                _session.Close();
                _session.Dispose();
            }
        }

        /// <summary>
        /// Dispose this object, when it is not longer needed.
        /// </summary>
        public void Dispose()
        {
            lock (m_sessions)
            {
                foreach (Session session in m_sessions.Values)
                {
                    session.Close();
                    session.Dispose();
                } // end foreach //
                m_sessions.Clear();
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Local dispose.
        /// </summary>
        /// <param name="intern">Set to <c>true</c> when calling from user code.</param>
        protected virtual void Dispose(bool intern)
        {
        }


    }
}
