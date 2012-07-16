//
// L2Header.cs
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
using System.Text;

namespace OpenAX25Contracts
{
	/// <summary>
	/// Source, destination and digi infos for a L2 frame.
	/// </summary>
	public struct L2Header
	{
		
		/// <summary>
		/// Build the header from the frame data.
		/// </summary>
		/// <param name="octets"></param>
		public L2Header(byte[] octets)
		{
			bool last;
			this.destination = new L2Callsign(octets, 0, out last);
			if (last)
				throw new L2InvalidAX25FrameException("Source field missing");
			this.source = new L2Callsign(octets, 7, out last);
			if (last) {
				this.digis = new L2Callsign[0];
				return;
			}
			IList<L2Callsign> list = new List<L2Callsign>();
			for (int i = 14; !last; i += 7)
				list.Add(new L2Callsign(octets, i, out last));
			this.digis = new L2Callsign[list.Count];
			list.CopyTo(this.digis, 0);
		}
		
		/// <summary>
		/// Build the header from address information.
		/// </summary>
		/// <param name="source">The source address.</param>
		/// <param name="destination">The destination address.</param>
		/// <param name="digis">The intermediate digis.</param>
		public L2Header(L2Callsign source, L2Callsign destination, L2Callsign[] digis)
		{
			this.source = source;
			this.destination = destination;
			this.digis = digis;			
		}
		
		/// <summary>
		/// The source of the frame (where it comes from).
		/// </summary>
		public readonly L2Callsign source;
		
		/// <summary>
		/// The destination of the frame (where it should arrive).
		/// </summary>
		public readonly L2Callsign destination;
		
		/// <summary>
		/// The intermediate digis on the path.
		/// </summary>
		public readonly L2Callsign[] digis;
		
		/// <summary>
		/// The next station to go.
		/// </summary>
		public L2Callsign nextHop
		{
			get {
				foreach (L2Callsign cs in this.digis)
					if (!cs.chBit)
						return cs;
				return this.destination;
			}
		}
		
		/// <summary>
		/// Get a string representation of this header.
		/// </summary>
		/// <returns>String representation of this header.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(this.destination.ToString());
			sb.Append(" <- ");
			sb.Append(this.source.ToString());
			if (this.digis.Length > 0) {
				sb.Append(" via");
				foreach (L2Callsign cs in this.digis) {
					sb.Append(' ');
					sb.Append(cs.ToString());
					if (cs.chBit)
						sb.Append('*');
				} // end foreach //
			}
			return sb.ToString();
		}
		
	}
}
