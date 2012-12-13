//
// ClientSession.cs
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
    internal sealed class ClientSession : IL3Channel
    {
        internal readonly Runtime m_runtime = Runtime.Instance;
        internal readonly Guid m_id;
        internal readonly string m_name;
        internal readonly IDictionary<string, string> m_properties;
        internal readonly LinkMultiplexerChannel m_multiplexer;
        internal readonly ClientEndpoint m_endpoint;
        internal readonly IL3Channel m_target;
        internal readonly L2Callsign m_callsign;
        internal readonly L2Callsign[] m_digis;

        internal ClientSession(
            LinkMultiplexerChannel multiplexer, ClientEndpoint endpoint, IL3Channel target,
            IDictionary<string, string> properties, string alias)
        {
            if (multiplexer == null)
                throw new ArgumentNullException("multiplexer");
            if (endpoint == null)
                throw new ArgumentNullException("endpoint");
            if (target == null)
                throw new ArgumentNullException("target");
            if (properties == null)
                throw new ArgumentNullException("properties");
            m_id = Guid.NewGuid();
            string _val;
            if (!properties.TryGetValue("RemoteAddr", out _val))
                throw new MissingPropertyException("RemoteAddr");
            m_callsign = new L2Callsign(_val);
            if (!properties.TryGetValue("Route", out _val))
                _val = "";
            string[] route = _val.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            m_digis = new L2Callsign[route.Length];
            for (int i = 0; i < route.Length; ++i)
                m_digis[i] = new L2Callsign(route[i]);
            m_name = endpoint.m_name + "/" + ((alias != null)?alias:target.Name);
            m_multiplexer = multiplexer;
            m_endpoint = endpoint;
            m_target = target;
            m_properties = properties;
        }

        /// <summary>
        /// Gets the name of the channel. This name have to be unique accross the
        /// application and can never change. There is no interpretion or syntax check
        /// performed.
        /// </summary>
        /// <value>
        /// The unique name of this channel.
        /// </value>
        public string Name { get { return m_name; } }

        /// <summary>
        /// Gets the properties of this channel.
        /// </summary>
        /// <value>
        /// The properties.
        /// </value>
        public IDictionary<string, string> Properties { get { return m_properties; } }

        /// <summary>
        /// Open the interface.
        /// </summary>
        public void Open()
        {
        }

        /// <summary>
        /// Close the interface.
        /// </summary>
        public void Close()
        {
        }

        /// <summary>
        /// Resets the channel. The data link is closed and reopened. All pending
        /// data is withdrawn.
        /// </summary>
        public void Reset()
        {
        }

        /// <summary>
        /// Get the target channel.
        /// </summary>
        public IL3Channel L3Target { get { return m_target; } }

        /// <summary>
        /// Send a primitive over the channel.
        /// </summary>
        /// <param name="message">The primitive to send.</param>
        /// <param name="expedited">Send expedited if set.</param>
        public void Send(IPrimitive message, bool expedited)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            if (!(message is LinkMultiplexerPrimitive))
                throw new Exception(String.Format("Invalid primitive of class {0}! Expected LinkMultiplexerPrimitive.",
                    message.GetType().Name));
            LinkMultiplexerPrimitive lmp = (LinkMultiplexerPrimitive)message;
            m_multiplexer.Input(lmp, expedited, this);
        }

        internal void SendToPeer(IPrimitive message, bool expedited)
        {
            if (m_runtime.LogLevel >= LogLevel.DEBUG)
                m_runtime.Log(LogLevel.DEBUG, m_name, "=Send " + message.GetType().Name);
            m_target.Send(message, expedited);
        }

        private ClientSession()
        {
            m_id = Guid.Empty;
            m_name = String.Empty;
            m_properties = new Dictionary<string, string>();
            m_multiplexer = null;
            m_endpoint = null;
            m_target = null;
            m_callsign = L2Callsign.CQ;
            m_digis = new L2Callsign[0];
        }

        internal static ClientSession NullSession = new ClientSession();
    }
}
