//
// L2Callsign.cs
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

namespace OpenAX25Contracts
{
	/// <summary>
	/// Hamradio callsign as used in packet radio.
	/// </summary>
	public struct L2Callsign
	{
		
		/// <summary>
		/// Get a callsign from a buffer at a specified index.
		/// </summary>
		/// <param name="octets">The octet buffer.</param>
		/// <param name="iStart">The position in the buffer where to read.</param>
		/// <param name="last"><c>true</c> if the EOA bit is set on this callsign.</param>
		public L2Callsign(byte[] octets, int iStart, out bool last)
		{
			if (octets == null)
				throw new ArgumentNullException("octets");
			int l = octets.Length;
			if ((iStart < 0) || (iStart + 7 > l))
				throw new ArgumentOutOfRangeException("octets");
			StringBuilder sb = new StringBuilder(6);
			for (int i = 0; i < 6; ++i) {
				byte b = octets[iStart + i];
				if ((b & 0x01) != 0x00)
					throw new InvalidAX25FrameException("EOA bit inside of callsign field");
				int _ch = ((int)b) >> 1;
				if (_ch == (int)' ')
					break;
				sb.Append((char)_ch);
			} // end for //
			this.callsign = sb.ToString();
			byte c = octets[iStart + 6];
			last = ((c & 0x01) != 0x00);
			this.chBit = ((c & 0x80) != 0x00);
			this.ssid = (int)((c & 0x1e) >> 1);
		}

        /// <summary>
        /// Construct a new callsign.
        /// </summary>
        /// <param name="callsign">The callsign without SSID.</param>
        /// <param name="ssid">The SSID.</param>
        /// <param name="chBit">The CH bit (Default: false).</param>
        public L2Callsign(string callsign, int ssid, bool chBit)
        {
            if (String.IsNullOrEmpty(callsign))
                throw new InvalidPropertyException("Empty callsign");
            if (callsign.Length > 6)
                throw new InvalidPropertyException("Callsign too long: \"" + callsign + "\"");
            this.callsign = callsign.ToUpperInvariant();
            if ((ssid < 0) || (ssid > 15))
                throw new InvalidPropertyException("SSID range error [0..15]: " + ssid);
            this.ssid = ssid;
            this.chBit = chBit;
        }

        /// <summary>
        /// Copy constructor with CH bit override.
        /// </summary>
        /// <param name="callsign">Source callsign.</param>
        /// <param name="chBit">The CH bit.</param>
        public L2Callsign(string callsign, bool chBit = false)
        {
            string _callsign;
            int _ssid;

            if (String.IsNullOrEmpty(callsign))
                throw new InvalidPropertyException("Empty callsign");
            int p = callsign.IndexOf('-');
            if (p >= 0)
            {
                _callsign = callsign.Substring(0, p);
                _ssid = (p + 1 < callsign.Length) ? (int)(callsign.ToCharArray()[p+1]-'0') : 0;
            }
            else
            {
                _callsign = callsign;
                _ssid = 0;
            }
            if (_callsign.Length > 6)
                throw new InvalidPropertyException("Callsign too long: \"" + _callsign + "\"");
            this.callsign = _callsign.ToUpperInvariant();
            if ((_ssid < 0) || (_ssid > 15))
                throw new InvalidPropertyException("SSID range error [0..15]: " + _ssid);
            this.ssid = _ssid;
            this.chBit = chBit;
        }

        /// <summary>
        /// Copy Constructor.
        /// </summary>
        /// <param name="callsign">Source callsign.</param>
        public L2Callsign(L2Callsign callsign)
        {
            this.callsign = callsign.callsign;
            this.ssid = callsign.ssid;
            this.chBit = callsign.chBit;
        }

        /// <summary>
        /// Copy  Constructor with CH bit override.
        /// </summary>
        /// <param name="callsign">Source callsign.</param>
        /// <param name="chBit">The CH bit.</param>
        public L2Callsign(L2Callsign callsign, bool chBit)
        {
            this.callsign = callsign.callsign;
            this.ssid = callsign.ssid;
            this.chBit = chBit;
        }
		
		/// <summary>
		/// The callsign without(!) ssid.
		/// </summary>
		public readonly string callsign;
		
		/// <summary>
		/// The ssid.
		/// </summary>
		public readonly int ssid;
		
		/// <summary>
		/// The C - bit (Poll / Final) or the H - bit (Has been repeated).
		/// </summary>
		public readonly bool chBit;
		
		/// <summary>
		/// Get string representation of this callsign.
		/// </summary>
		/// <returns>String representation of the callsign.</returns>
		public override string ToString()
		{
			return String.Format("{0}-{1}", this.callsign, this.ssid);
		}
		
	}
}
