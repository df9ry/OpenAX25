﻿//
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

using OpenAX25Contracts;

namespace OpenAX25_DuplexPhysicalLayer
{
	/// <summary>
	/// ChannelFactory for Duplex Physical Layer.
	/// </summary>
	public class ChannelFactory : IChannelFactory
	{

		/// <summary>
		/// ChannelFactory constructor.
		/// </summary>
		public ChannelFactory()	{}
		
		/// <summary>
		/// The channel class name of this Duplex Physical Layer implementation.
        /// <c>DPXPH:799c4953-42d7-4311-9f3c-8d64bfda4bc6</c>
		/// </summary>
		public string ChannelClass
		{
			get {
                return "DPXPH:799c4953-42d7-4311-9f3c-8d64bfda4bc6";
			}
		}
		
		/// <summary>
		/// Create a new Duplex Physical Layer channel.
		/// </summary>
		/// <param name="properties">Properties for this Duplex Physical Layer channel.</param>
		/// <returns>New Duplex Physical Layer channel.</returns>
		public IChannel CreateChannel(IDictionary<string,string> properties)
		{
			return new DuplexPhysicalLayerChannel(properties);
		}
		
	}
}
