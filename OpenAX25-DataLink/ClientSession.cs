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
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25_DataLink
{
    internal sealed class ClientSession : L3Channel
    {
        internal readonly Guid m_id = Guid.NewGuid();
        internal readonly DataLinkChannel m_dataLink;
        internal readonly ClientEndpoint m_endpoint;
        internal readonly Configuration m_config;
        internal readonly DataLinkStateMachine m_machine;
        internal readonly L2Callsign m_defaultCallsign;
        internal readonly L2Callsign[] m_defaultDigis;

        internal L2Callsign m_callsign;
        internal L2Callsign[] m_digis;
        internal IL3Channel m_linkMultiplexer = null;
        private static int no;

        internal ClientSession(
            DataLinkChannel dataLink, ClientEndpoint endpoint, IL3Channel target,
            IDictionary<string, string> properties, string alias)
            : base(properties, dataLink.m_name + ":" + alias, true, target)
        {
            if (dataLink == null)
                throw new ArgumentNullException("multiplexer");
            if (endpoint == null)
                throw new ArgumentNullException("endpoint");
            string _val;
            if (!properties.TryGetValue("RemoteAddr", out _val))
                throw new MissingPropertyException("RemoteAddr");
            m_defaultCallsign = new L2Callsign(_val);
            m_callsign = m_defaultCallsign;
            if (!properties.TryGetValue("Route", out _val))
                _val = "";
            string[] route = _val.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            m_defaultDigis = new L2Callsign[route.Length];
            for (int i = 0; i < route.Length; ++i)
                m_defaultDigis[i] = new L2Callsign(route[i]);
            m_digis = m_defaultDigis;
            m_dataLink = dataLink;
            m_endpoint = endpoint;
            m_config = new Configuration(m_name);
            m_config.Header = new AX25Header(m_endpoint.m_callsign,
                m_callsign, m_digis);
            if (properties.TryGetValue("N1", out _val))
                try
                {
                    m_config.Initial_N1 = Int64.Parse(_val);
                    if ((m_config.Initial_N1 < 16) || (m_config.Initial_N1 > 32787))
                        throw new ArgumentOutOfRangeException("16 .. 32767");
                }
                catch (Exception e)
                {
                    throw new InvalidPropertyException("N1", e);
                }
            if (properties.TryGetValue("N2", out _val))
                try
                {
                    m_config.Initial_N2 = Int64.Parse(_val);
                    if ((m_config.Initial_N2 < 0) || (m_config.Initial_N2 > 256))
                        throw new ArgumentOutOfRangeException("0 .. 16");
                }
                catch (Exception e)
                {
                    throw new InvalidPropertyException("N2", e);
                }
            if (properties.TryGetValue("SAT", out _val))
                try
                {
                    m_config.Initial_SAT = Int64.Parse(_val);
                    if ((m_config.Initial_SAT < 10) || (m_config.Initial_SAT > 3600000))
                        throw new ArgumentOutOfRangeException("10 .. 3600000");
                }
                catch (Exception e)
                {
                    throw new InvalidPropertyException("SAT", e);
                }
            if (properties.TryGetValue("SRT", out _val))
                try
                {
                    m_config.Initial_SRT = Int64.Parse(_val);
                    if ((m_config.Initial_SRT < 10) || (m_config.Initial_SRT > 600000))
                        throw new ArgumentOutOfRangeException("10 .. 600000");
                }
                catch (Exception e)
                {
                    throw new InvalidPropertyException("SRT", e);
                }
            if (properties.TryGetValue("Version", out _val))
                try
                {
                    if ("2.0".Equals(_val))
                        m_config.Initial_version = AX25Version.V2_0;
                    else if ("2.2".Equals(_val))
                        m_config.Initial_version = AX25Version.V2_2;
                    else
                        throw new ArgumentOutOfRangeException("2.0|2.2");
                }
                catch (Exception e)
                {
                    throw new InvalidPropertyException("Version", e);
                }
            int n = NewNo();
            string name = "**" + n + "**";
            m_runtime.Log(LogLevel.INFO, m_name, "Assigning state machine " + name);
            m_machine = new DataLinkStateMachine(m_config, name);
            m_machine.OnDataLinkOutputEvent +=
                new OnDataLinkOutputEventHandler(OnDataLinkOutput);
            m_machine.OnLinkMultiplexerOutputEvent +=
                new OnLinkMultiplexerOutputEventHandler(OnLinkMultiplexerOutput);
        }

        private static int NewNo()
        {
            lock (typeof(ClientSession))
            {
                return ++ no;
            }
        }

        /// <summary>
        /// Open the interface.
        /// </summary>
        public override void Open()
        {
            if (m_linkMultiplexer != null)
                return;
            lock (this)
            {
                m_linkMultiplexer = m_endpoint.m_remoteEndpoint.Bind(this, m_properties, m_name);
                base.Open();
            } // end lock //
        }

        /// <summary>
        /// Close the interface.
        /// </summary>
        public override void Close()
        {
            if (m_linkMultiplexer == null)
                return;
            lock (this)
            {
                base.Close();
                try
                {
                    m_endpoint.m_remoteEndpoint.Unbind(m_linkMultiplexer);
                }
                catch (Exception e)
                {
                    m_runtime.Log(LogLevel.WARNING, m_name,
                        "Error unregister local endpoint: " + e.Message);
                }
                finally
                {
                    m_linkMultiplexer = null;
                }
            } // end lock //
        }

        /// <summary>
        /// Send a primitive over the channel.
        /// </summary>
        /// <param name="message">The primitive to send.</param>
        /// <param name="expedited">Send expedited if set.</param>
        protected override void Input(IPrimitive message, bool expedited)
        {
            lock (this)
            {
                if (message == null)
                    throw new ArgumentNullException("message");
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "RX " + message.GetType().Name);
                if (message is PhysicalLayerPrimitive)
                    m_machine.Input((PhysicalLayerPrimitive)message);
                else if (message is LinkMultiplexerPrimitive)
                    m_machine.Input((LinkMultiplexerPrimitive)message);
                else if (message is DataLinkPrimitive)
                    m_machine.Input((DataLinkPrimitive)message);
                else
                    throw new Exception(String.Format(
                        "Invalid primitive of class {0}! Expected PhysicalLayerPrimitive or " +
                        "LinkMultiplexerPrimitive or DataLinkPrimitive", message.GetType().Name));
            } // end lock //
        }

        private void OnDataLinkOutput(DataLinkPrimitive p)
        {
            lock (this)
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, "TX " + p.DataLinkPrimitiveTypeName);

                if (p.DataLinkPrimitiveType == DataLinkPrimitive_T.DL_CONNECT_Indication_T)
                {
                    DL_CONNECT_Indication rq = (DL_CONNECT_Indication)p;
                    string[] _path = rq.RemoteAddr.Split(new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);
                    if (_path.Length == 0)
                    {
                        m_callsign = m_defaultCallsign;
                        m_digis = m_defaultDigis;
                    }
                    else
                    {
                        m_callsign = new L2Callsign(_path[0]);
                        m_digis = new L2Callsign[_path.Length - 1];
                        for (int i = 0; i < _path.Length - 2; ++i)
                            m_digis[i] = new L2Callsign(_path[i + 1]);
                    }
                }

                if (m_target != null)
                    m_target.Send(p);
                else
                    m_runtime.Log(LogLevel.WARNING, m_name, "DATA DROP (Target is null)");

                if (p.DataLinkPrimitiveType == DataLinkPrimitive_T.DL_DISCONNECT_Confirm_T)
                {
                    m_callsign = m_defaultCallsign;
                    m_digis = m_defaultDigis;
                }
            } // end lock //
        }

        private void OnLinkMultiplexerOutput(LinkMultiplexerPrimitive p)
        {
            lock (this)
            {
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                {
                    if (p.LinkMultiplexerPrimitiveType == LinkMultiplexerPrimitive_T.LM_DATA_Request_T)
                        m_runtime.Log(LogLevel.DEBUG, m_name, "TX LM_DATA_Request " +
                            ((LM_DATA_Request)p).Frame.ToString());
                    else
                        m_runtime.Log(LogLevel.DEBUG, m_name, "TX " +
                            p.LinkMultiplexerPrimitiveTypeName);
                }
                if (m_linkMultiplexer != null)
                    m_linkMultiplexer.Send(p);
                else
                    m_runtime.Log(LogLevel.WARNING, m_name, "DATA DROP (LinkMultiplexer is null)");
            } // end lock //
        }

    }
}
