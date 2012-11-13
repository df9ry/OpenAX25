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

namespace OpenAX25Core
{
    /// <summary>
    /// Standard Implementation of a Channel.
    /// </summary>
    public abstract class Channel : IChannel
    {
        /// <summary>
        /// Properties of this channel.
        /// </summary>
        protected readonly IDictionary<string, string> m_properties;

        /// <summary>
        /// Name of this channel.
        /// </summary>
        protected readonly string m_name;

        /// <summary>
        /// The runtime objcect.
        /// </summary>
        protected Runtime m_runtime = Runtime.Instance;

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
		/// </item>
		/// </list>
		/// </param>
        /// <param name="alias">Name alias for better tracing [Default: Value of "Name"]</param>
        /// <param name="suppressRegistration">
        /// If set no registration in the runtime is performed (For proxies).</param>
		protected Channel(IDictionary<string, string> properties, string alias = null,
            bool suppressRegistration = false)
		{
            if (properties == null)
                throw new ArgumentNullException("properties");
            this.m_properties = properties;
            if (String.IsNullOrEmpty(alias))
            {
                if (!properties.TryGetValue("Name", out this.m_name))
                    throw new MissingPropertyException("Name");
                if (String.IsNullOrEmpty(this.m_name))
                    throw new InvalidPropertyException("Name");
            }
            else
            {
                this.m_name = alias;
            }
            if (!suppressRegistration)
                Runtime.Instance.RegisterChannel(this, m_name);
		}

        /// <summary>
        /// Name of this interface.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return this.m_name;
            }
        }

        /// <summary>
        /// Properties of this interface.
        /// </summary>
        public virtual IDictionary<string, string> Properties
        {
            get
            {
                return this.m_properties;
            }
        }

        /// <summary>
        /// Reset the channel and withdraw all frames in the
        /// receiver and transmitter queue.
        /// </summary>
        public virtual void Reset()
        {
            m_runtime.Log(LogLevel.INFO, m_name, "Channel reset");
            lock (this)
            {
                this.Close();
                this.Open();
            }
        }

        /// <summary>
        /// Open the channel, so that data actually be transmitted and received.
        /// </summary>
        public virtual void Open()
        {
            m_runtime.Log(LogLevel.INFO, m_name, "Channel opened");
        }

        /// <summary>
        /// Close the channel. No data will be transmitted or received. All queued
        /// data is preserved.
        /// </summary>
        public virtual void Close()
        {
            m_runtime.Log(LogLevel.INFO, m_name, "Channel closed");
        }

    }
}
