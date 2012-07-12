//
// L2HexConverter.cs
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
using System.Text;

namespace OpenAX25Core
{
	/// <summary>
	/// Utility class for hexadecimal number handling.
	/// </summary>
	public class L2HexConverter
	{	
		
		/// <summary>
		/// Hexadecimal digits in lower case.
		/// </summary>
		public static readonly char[] HEX_lc = new char[] {
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'
		};
		
		/// <summary>
		/// Hexadecimal digits in upper case.
		/// </summary>
		public static readonly char[] HEX_uc = new char[] {
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
		};
		
		public static string ToHexString(byte[] data)
		{
			if (data == null)
				return "<null>";
			StringBuilder sb = new StringBuilder(data.Length * 3);
			foreach (byte b in data) {
				if (sb.Length > 0)
					sb.Append(' ');
				sb.Append(HEX_lc[b/16]);
				sb.Append(HEX_lc[b%16]);
			} // end foreach //
			return sb.ToString();
		}
		
		private L2HexConverter() // No instances allowed.
		{
		}
		
	}
}
