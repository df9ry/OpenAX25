//
// AXUDPChannel.cs
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

namespace OpenAX25Core
{
	/// <summary>
	/// CRC_B encoding This annex is provided for explanatory purposes and indicates
	/// the bit patterns that will exist in the physical layer. It is included for
	/// the purpose of checking an ISO/IEC 14443-3 Type B implementation of CRC_B
	/// encoding. Refer to ISO/IEC 3309 and CCITT X.25 2.2.7 and V.42 8.1.1.6.1 for
	/// further details. Initial Value = 'FFFF'
	/// </summary>
	public class CrcB
	{
	    const ushort __crcBDefault = 0xffff;
	
	    private static ushort UpdateCrc(byte b, ushort crc)
	    {
	    	unchecked
	        {
	            byte ch = (byte)(b^(byte)(crc & 0x00ff));
	            ch = (byte)(ch ^ (ch << 4));
	            return (ushort)((crc >> 8)^(ch << 8)^(ch << 3)^(ch >> 4));
	        }
	    }
	
	    /// <summary>
	    /// Compute the checksum for a data block.
	    /// </summary>
	    /// <param name="bytes">Data block.</param>
	    /// <returns>Checksum for data block.</returns>
	    public static ushort ComputeCrc(byte[] bytes)
	    {
	    	unchecked
	    	{
	            var res = __crcBDefault;
	            foreach (var b in bytes)
	                    res = UpdateCrc(b, res);
	            return (ushort)~res;
	    	}
	    }
	}
}
