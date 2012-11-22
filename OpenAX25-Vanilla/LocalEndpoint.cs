using System;
using OpenAX25Contracts;
using OpenAX25Core;
using System.Collections.Generic;

namespace OpenAX25_Vanilla
{
    internal sealed class LocalEndpoint : ILocalEndpoint, IDisposable
    {
        internal readonly VanillaChannel m_channel;
        internal readonly ILocalEndpoint m_targetLocalEndpoint;

        internal LocalEndpoint(VanillaChannel channel, ILocalEndpoint targetLocalEndpoint)
        {
            m_channel = channel;
            m_targetLocalEndpoint = targetLocalEndpoint;
        }

        /// <summary>
        /// Get unique ID of the endpoint.
        /// </summary>
        public Guid Id { get { return m_targetLocalEndpoint.Id; } }

        /// <summary>
        /// Get the address of this endpoint.
        /// </summary>
        public string Address { get { return m_targetLocalEndpoint.Address; } }

        /// <summary>
        /// Dispose this object, when it is not longer needed.
        /// </summary>
        public void Dispose()
        {
            //Close();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Local dispose.
        /// </summary>
        /// <param name="intern">Set to <c>true</c> when calling from user code.</param>
        private void Dispose(bool intern)
        {
        }

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
            Session session = new Session(this, receiver);
            session.Open();
            return session;
        }

        /// <summary>
        /// Unattach a session that where previously registered.
        /// </summary>
        /// <param name="transmitter">
        /// Tranmitter returned from previous call to Bind.
        /// </param>
        public void Unbind(IL3Channel transmitter)
        {
            if (!(transmitter is Session))
                throw new Exception(
                    "Invalid session in OpenAX25_Vanilla.LocalEndpoint.Unbind(session)");
            Session session = (Session)transmitter;
            session.Close();
            session.Dispose();
        }

    }
}
