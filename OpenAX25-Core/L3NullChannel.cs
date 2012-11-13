//
// L3NullChannel
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

using System.Collections.Generic;
using OpenAX25Contracts;

namespace OpenAX25Core
{
    /// <summary>
    /// The dummy channel.
    /// </summary>
    class L3NullChannel : IL3Channel
    {
		private IDictionary<string,string> m_properties = new Dictionary<string,string>();
		
		internal L3NullChannel()
		{
			m_properties.Add("Name", Name);
		}
		
		/// <summary>
		/// Gets the name of the channel. This name have to be unique accross the
		/// application and can never change. There is no interpretion or syntax check
		/// performed.
		/// </summary>
		/// <value>
		/// The unique name of this channel.
		/// </value>
		public string Name { get { return "L3NULL"; } }

		/// <summary>
		/// Gets the properties of this channel.
		/// </summary>
		/// <value>
		/// The properties.
		/// </value>
		public IDictionary<string,string> Properties { get { return m_properties; } }

        /// <summary>
        /// Get or set the target channel for received data.
        /// </summary>
        public IL3Channel L3Target { get { return null; } set {} }
		
		/// <summary>
		/// Open the interface.
		/// </summary>
		public void Open() {}
		
		/// <summary>
		/// Close the interface.
		/// </summary>
		public void Close() {}

		/// <summary>
		/// Resets the channel. The data link is closed and reopened. All pending
		/// data is withdrawn.
		/// </summary>
		public void Reset() {}

        /// <summary>
        /// Send message over the channel.
        /// </summary>
        public void Send(DataLinkPrimitive _) {}

    }
}
