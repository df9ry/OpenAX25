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

namespace OpenAX25_TCPServer
{
	public class ChannelFactory : IChannelFactory
	{

		/// <summary>
		/// ChannelFactory constructor.
		/// </summary>
		public ChannelFactory()	{}
		
		/// <summary>
		/// The channel class name of this KISS implementation.
		/// <c>TCPSERVER:3a97ecf0-6740-4478-92a9-539b293ec59d</c>
		/// </summary>
		public string ChannelClass
		{
			get {
				return "TCPSERVER:3a97ecf0-6740-4478-92a9-539b293ec59d";
			}
		}
		
		/// <summary>
		/// Create a new TCPServer channel.
		/// </summary>
		/// <param name="properties">Proeprties for this TCPServer channel.</param>
		/// <returns>New AXUDP channel.</returns>
		public IChannel CreateChannel(IDictionary<string,string> properties)
		{
			return new TCPServerChannel(properties);
		}
		
    }
}
