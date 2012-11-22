//
// ChannelFactory.cs
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

namespace OpenAX25_Console
{
	public class ChannelFactory : IChannelFactory
	{

		/// <summary>
		/// ChannelFactory constructor.
		/// </summary>
		public ChannelFactory()	{}
		
		/// <summary>
		/// The channel class name of this CONSOLE implementation.
        /// <c>CONSOLE:c9cfa288-d3a8-4ada-82ef-3d231c966dd7</c>
		/// </summary>
		public string ChannelClass
		{
			get {
                return "CONSOLE:c9cfa288-d3a8-4ada-82ef-3d231c966dd7";
			}
		}
		
		/// <summary>
		/// Create a new TCPServer channel.
		/// </summary>
		/// <param name="properties">Proeprties for this CONSOLE channel.</param>
		/// <returns>New CONSOLE channel.</returns>
		public IChannel CreateChannel(IDictionary<string,string> properties)
		{
			return new ConsoleChannel(properties);
		}
		
    }
}
