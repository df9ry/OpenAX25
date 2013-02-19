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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using OpenAX25Contracts;

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
		/// <item><term>Target</term><description>Where to route packages to [Default: NULL]</description></item>
		/// </item>
		/// </list>
		/// </param>
        /// <param name="alias">Name alias for better tracing [Default: Value of "Name"]</param>
        /// <param name="suppressRegistration">
        /// If set no registration in the runtime is performed (For proxies).</param>
        /// <param name="target">If set, use this as the target of this object.</param>
        protected L3Channel(IDictionary<string, string> properties, string alias = null,
            bool suppressRegistration = true, IL3Channel target = null)
            : base(properties, alias, suppressRegistration)
		{
            if (target == null)
            {
                string _target;
                if (properties.TryGetValue("Target", out _target))
                {
                    m_target = m_runtime.LookupL3Channel(_target);
                    if (m_target == null)
                        throw new InvalidPropertyException("Target not found: " + _target);
                }
            }
            else
            {
                m_target = target;
            }
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
        /// <param name="p">Data Link primitive to send</param>
        /// <param name="expedited">Send expedited if set.</param>
        public virtual void Send(IPrimitive p, bool expedited = true)
        {
            lock (this)
            {
                m_queue.Add(new Entry(p, expedited));
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
        /// Check if this is the null channel.
        /// </summary>
        /// <returns>True, if this is the null channel.</returns>
        public bool IsNullChannel()
        {
            return IsNullChannel(this);
        }

        /// <summary>
        /// Check if a channel is the null channel.
        /// </summary>
        /// <param name="ch">The channel to check.</param>
        /// <returns>True, if ch is null or the null channel.</returns>
        public static bool IsNullChannel(IL3Channel ch)
        {
            return ((ch == null) || (ch.GetHashCode() == L3NullChannel.Instance.GetHashCode()));
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
                    Input(e.p, e.expedited);
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
        /// <param name="p">The message to process.</param>
        /// <param name="expedited">Send express if set.</param>
        protected virtual void Input(IPrimitive p, bool expedited)
        {
            m_runtime.Log(LogLevel.WARNING, m_name, "Input Method not implemented");
            throw new Exception(
                "L3Channel.Input(IPrimitive p, bool expedited) is not implemented in channel \""
                + m_name + "\"");
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
