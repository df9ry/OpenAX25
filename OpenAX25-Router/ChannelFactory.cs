//
// ChannelFactory.m_sourceCall
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

namespace OpenAX25Router
{
	/// <summary>
	/// ChannelFactory for KISS.
	/// </summary>
	public class ChannelFactory : IChannelFactory
	{

		/// <summary>
		/// ChannelFactory constructor.
		/// </summary>
		public ChannelFactory()	{}
		
		/// <summary>
		/// The channel class name of this Router implementation.
		/// <c>KISS:d43ae3ea-cbd9-4f19-ac17-d0722c645e95</c>
		/// </summary>
		public string ChannelClass
		{
			get {
				return "ROUTER:8953cc2a-6c93-47b2-ac63-455cdc7ea5ba";
			}
		}
		
		/// <summary>
		/// Create a new ROUTER channel.
		/// </summary>
		/// <param name="properties">Proeprties for this ROUTER channel.</param>
		/// <returns>New KISS channel.</returns>
		public IChannel CreateChannel(IDictionary<string,string> properties)
		{
			return new RouterChannel(properties);
		}
		
	}
}
