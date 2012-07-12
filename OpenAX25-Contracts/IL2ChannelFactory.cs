//
// IL2ChannelFactory.cs
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

namespace OpenAX25Contracts
{
	/// <summary>
	/// Factory to create channel instances.
	/// </summary>
	public interface IL2ChannelFactory
	{
		
		/// <summary>
		/// The channel class name (e.g. "KISS:" + &gt;GUID&lt; for a Kiss interface);
		/// </summary>
		string ChannelClass { get; }
		
		/// <summary>
		/// Create a new instance of the channel, using the given properties.
		/// </summary>
		/// <param name="properties">Properties for the new channel instance.</param>
		/// <returns>New channel instance.</returns>
		IL2Channel CreateChannel(IDictionary<string,string> properties);
	}
}
