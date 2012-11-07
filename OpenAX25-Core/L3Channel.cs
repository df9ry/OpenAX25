//
// L2Channel.cs
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
using System.Collections.Concurrent;
using System.Threading;

namespace OpenAX25Core
{
    /// <summary>
    /// Standard Implementation of a L3Channel.
    /// </summary>
    public abstract class L3Channel : Channel, IL3Channel, IDisposable
    {
        /// <summary>
        /// Default target channel.
        /// </summary>
        protected IL3Channel m_target = null;

        /// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="properties">Properties of the channel.
		/// <list type="bullet">
		/// <listheader>
		/// <term>Property name</term>
		/// <description>Description</description>
		/// </listheader>
		/// <item><term>Name</term><description>Name of the channel [Mandatory]</description>
		/// <item><term>Target</term><description>Where to route packages to [Default: PROTO]</description></item>
		/// </item>
		/// </list>
		/// </param>
        /// <param name="suppressRegistration">
        /// If set no registration in the runtime is performed (For proxies).</param>
        protected L3Channel(IDictionary<string, string> properties, bool suppressRegistration = false)
            : base(properties, suppressRegistration)
		{
            string _target;
            if (!properties.TryGetValue("Target", out _target))
                _target = "PROTO";
            m_target = m_runtime.LookupL3Channel(_target);
			if (m_target == null)
				throw new InvalidPropertyException("Target not found: " + _target);
		}
		
        /// <summary>
        /// Get or set the default target channel.
        /// </summary>
        public virtual IL3Channel L3Target
        {
            get
            {
                return this.m_target;
            }
            set
            {
                this.m_target = value;
            }
        }

        /// <summary>
        /// Reset the channel and withdraw all frames in the
        /// receiver and transmitter queue.
        /// </summary>
        public override void Reset()
        {
            m_runtime.Log(LogLevel.INFO, m_name, "Channel reset");
            lock (this)
            {
                this.Close();
                while (m_queue.Count > 0)
                    m_queue.Take();
                this.Open();
            }
        }

        /// <summary>
        /// Open the channel, so that data actually be transmitted and received.
        /// </summary>
        public override void Open()
        {
            lock (this)
            {
                if (m_thread != null) // Already open.
                    return;
                base.Open();
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
                if (m_thread == null) // Already closed.
                    return;
                m_thread.Abort();
                m_thread = null;
                base.Close();
            }
        }

        /// <summary>
        /// Send a Data Link primitive to the serving object.
        /// </summary>
        /// <param name="sender">Sender of the primitive</param>
        /// <param name="p">Data Link primitive to send</param>
        public virtual void Send(ILocalEndpoint sender, DataLinkPrimitive p)
        {
            lock (this)
            {
                m_queue.Add(new Entry(sender,p));
            }
        }

        /// <summary>
        /// Dispose this object, when it is not longer needed.
        /// </summary>
        public void Dispose()
        {
            Close();
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

        /// <summary>
        /// Thread routine. Can be overriden.
        /// </summary>
        protected virtual void Run()
        {
            m_runtime.Log(LogLevel.INFO, m_name, "Consumer thread started");
            while (true)
            {
                try
                {
                    Entry e = m_queue.Take();
                    Input(e.ep, e.p);
                }
                catch (Exception e)
                {
                    m_runtime.Log(LogLevel.ERROR, m_name, "Exception in consumer thread: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Method to process input message. Must be overriden.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="p">The message to process.</param>
        protected virtual void Input(ILocalEndpoint sender, DataLinkPrimitive p)
        {
            m_runtime.Log(LogLevel.WARNING, m_name, "Input Method not implemented");
            throw new Exception(
                "L3Channel.Input(ILocalEndpoint sender, DataLinkPrimitive p) is not implemented in channel \""
                + m_name + "\"");
        }

        private struct Entry
        {
            internal Entry(ILocalEndpoint _ep, DataLinkPrimitive _p)
            {
                ep = _ep;
                p = _p;
            }
            internal readonly ILocalEndpoint ep;
            internal readonly DataLinkPrimitive p;
        }

        private BlockingCollection<Entry> m_queue = new BlockingCollection<Entry>(
            new ConcurrentQueue<Entry>());
        private Thread m_thread = null;
    }
}
