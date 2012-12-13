//
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
using System.Linq;
using System.Text;
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25_LinkMultiplexer
{
    internal sealed class ClientEndpoint : ILocalEndpoint
    {
        internal readonly Guid m_id = Guid.NewGuid();
        internal readonly Runtime m_runtime = Runtime.Instance;
        internal readonly string m_name;
        internal readonly LinkMultiplexerChannel m_multiplexer;
        internal readonly L2Callsign m_callsign;
        internal readonly IDictionary<L2Callsign, ClientSession> m_sessions =
            new Dictionary<L2Callsign, ClientSession>();

        internal ClientEndpoint(LinkMultiplexerChannel multiplexer, L2Callsign callsign)
        {
            m_multiplexer = multiplexer;
            m_callsign = callsign;
            m_name = multiplexer.Name + "/" + callsign.ToString();
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
                ClientSession session = new ClientSession(m_multiplexer, this, receiver, properties, alias);
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
                } // end for //
                m_sessions.Clear();
            }
        }

        internal ClientSession GetSession(AX25Header header)
        {
            lock (this)
            {
                L2Callsign destination = header.Destination;
                if (!m_callsign.Equals(destination))
                    return null;
                L2Callsign source = header.Source;
                ClientSession cs;
                if (!m_sessions.TryGetValue(source, out cs))
                    return null;
                int lDigis = cs.m_digis.Length;
                L2Callsign[] digis = header.Digis;
                if (lDigis != digis.Length)
                    return null;
                for (int i = 0; i < lDigis; ++i)
                    if (!cs.m_digis[i].Equals(digis[lDigis - i]))
                        return null;
                return cs;
            }
        }
    }
}
