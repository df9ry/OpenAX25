//
//  SamplePacket.cs
//
//  Author:
//       Tania Knoebl (DF9RY) <DF9RY@DARC.de>
//
//  Copyright © 2012 Copyright © 2012 by Tania Knoebl - GNU LGPL applies
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
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;

namespace OpenAX25GUI
{
	internal class SamplePacket
	{
		internal static byte[] Frame {
			get {
				return SamplePacket.packet;
			}
		}

		private SamplePacket ()	{} // No instances allowed

		private const byte SABME = 0x6f; // SABME Command
		private const byte SABM  = 0x2f; // SABM Command
		private const byte DISC  = 0x43; // DISC Command
		private const byte DM    = 0x0f; // DM Response
		private const byte UA    = 0x63; // UA Reponse
		private const byte FRMR  = 0x87; // FRMR Response
		private const byte UI    = 0x03; // UI Command or Respose
		private const byte XID   = 0xcf; // XID Command or Response
		private const byte TEST  = 0xe3; // TEST Command or Response
		
		private const byte POLL  = 0x10; // Poll bit
		private const byte EOAF  = 0x01; // End Of Address Field
		private const byte CBIT  = 0x80; // "C" Bit
		
		private static byte[] packet = new byte[]
		{
			0x00, // KISS data follows
			GetAddrLetter('D'), // A1 
			GetAddrLetter('F'), // A2
			GetAddrLetter('9'), // A3
			GetAddrLetter('R'), // A4
			GetAddrLetter('Y'), // A5
			GetAddrLetter(' '), // A6
			GetSsidByte(9, /*isSource=*/false, /*isCommand=*/true, /*endOfAddr=*/false),
			GetAddrLetter('D'), // A7 
			GetAddrLetter('A'), // A8
			GetAddrLetter('0'), // A9
			GetAddrLetter('A'), // A10
			GetAddrLetter('A'), // A11
			GetAddrLetter('A'), // A12
			GetSsidByte(1, /*isSource=*/true, /*isCommand=*/true, /*endOfAddr=*/true),
			SABM | POLL
		};
		
		private static byte GetAddrLetter(char letter)
		{
			return (byte) ( (int) Char.ToUpper(letter) << 1 );
		}
		
		private static byte GetSsidByte(int ssid, bool isSource, bool isCommand, bool endOfAddr)
		{
			if ((ssid < 0) || (ssid > 15))
				throw new ArgumentOutOfRangeException("0 >= ssid < 16");
			return (byte) ( /*Unused bits*/0x60 | ( ssid << 1 ) |
				( (isSource != isCommand)?CBIT:0 ) | ( (endOfAddr)?EOAF:0 ) );
		}

	}
}

