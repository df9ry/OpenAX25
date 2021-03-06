﻿//
// ClientEndpoint.cs
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
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25_DataLink
{
    internal sealed class ClientEndpoint : ILocalEndpoint
    {
        internal readonly Guid m_id = Guid.NewGuid();
        internal readonly Runtime m_runtime = Runtime.Instance;
        internal readonly string m_name;
        internal readonly DataLinkChannel m_dataLink;
        internal readonly L2Callsign m_callsign;
        internal readonly IDictionary<L2Callsign, ClientSession> m_sessions =
            new Dictionary<L2Callsign, ClientSession>();

        internal ILocalEndpoint m_remoteEndpoint = null;

        internal ClientEndpoint(DataLinkChannel dataLink, L2Callsign callsign)
        {
            m_dataLink = dataLink;
            m_callsign = callsign;
            m_name = dataLink.Name + "/" + callsign.ToString();
        }

        /// <summary>
        /// Get unique ID of the endpoint.
        /// </summary>
        public Guid Id { get { return m_id; } }

        /// <summary>
        /// Get the address of this endpoint.
        /// </summary>
        public string Address { get { return m_callsign.ToString(); } }

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
        public IL3Channel Bind(IL3Channel receiver, IDictionary<string, string> properties, string alias)
        {
            if (receiver == null)
                throw new ArgumentNullException("receiver");
            lock (this)
            {
                ClientSession session = new ClientSession(m_dataLink, this, receiver, properties, alias);
                if (m_sessions.ContainsKey(session.m_callsign))
                    throw new DuplicateNameException(session.m_callsign.ToString());
                m_sessions.Add(session.m_callsign, session);
                session.Open();
                return session;
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
            if (transmitter.GetType() != typeof(ClientSession))
                throw new Exception(String.Format("Invalid class. Expected {0}, was {1}",
                    typeof(ClientSession).Name, transmitter.GetType().Name));
            ClientSession session;
            lock (this)
            {
                if (!m_sessions.TryGetValue(((ClientSession)transmitter).m_callsign, out session))
                    throw new NotFoundException(((ClientSession)transmitter).m_callsign.ToString());
                m_sessions.Remove(session.m_callsign);
                session.Close();
            } // end lock //
        }

        internal void Open()
        {
            if (m_remoteEndpoint != null)
                return;
            lock (this)
            {
                m_remoteEndpoint = m_dataLink.m_dataLinkProvider.RegisterLocalEndpoint(
                    m_callsign.ToString(), m_dataLink.Properties);
            }
        }

        internal void Close()
        {
            lock (this)
            {
                foreach (ClientSession cs in m_sessions.Values)
                {
                    try
                    {
                        cs.Close();
                    }
                    catch (Exception e)
                    {
                        m_runtime.Log(LogLevel.WARNING, m_name,
                            "Error closing session " + cs.Name + ": " + e.Message);
                    }
                } // end foreach //
                m_sessions.Clear();
                if (m_remoteEndpoint != null)
                    try
                    {
                        m_dataLink.m_dataLinkProvider.UnregisterLocalEndpoint(m_remoteEndpoint);
                    }
                    catch (Exception e)
                    {
                        m_runtime.Log(LogLevel.WARNING, m_name,
                            "Error unregister local endpoint: " + e.Message);
                    }
                    finally
                    {
                        m_remoteEndpoint = null;
                    }
            } // end lock //
        }

    }
}
