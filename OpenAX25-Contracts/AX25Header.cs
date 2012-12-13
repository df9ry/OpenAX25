//
// AX25Header.cs
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
	public struct AX25Header
	{
		
		/// <summary>
		/// Build the header from the frame data.
		/// </summary>
		/// <param name="frame"></param>
		public AX25Header(byte[] frame)
		{
			bool last;
			this.Destination = new L2Callsign(frame, 0, out last);
            this.IsCommand = this.Destination.chBit;
            if (last)
				throw new InvalidAX25FrameException("Source field missing");
			this.Source = new L2Callsign(frame, 7, out last);
            this.IsResponse = this.Source.chBit;
            if (last)
            {
				this.Digis = new L2Callsign[0];
                Length = 14;
				return;
			}
			IList<L2Callsign> digiList = new List<L2Callsign>();
            int i;
			for (i = 14; !last; i += 7)
				digiList.Add(new L2Callsign(frame, i, out last));
			this.Digis = new L2Callsign[digiList.Count];
			digiList.CopyTo(this.Digis, 0);
            Length = i;
		}
		
		/// <summary>
		/// Build the header from address information.
		/// </summary>
		/// <param name="source">The source address.</param>
		/// <param name="destination">The destination address.</param>
		/// <param name="digis">The intermediate digis.</param>
        public AX25Header(L2Callsign source, L2Callsign destination, L2Callsign[] digis = null)
		{
            if (digis == null)
                digis = new L2Callsign[0];
			this.Source = source;
            this.IsResponse = this.Source.chBit;
            this.Destination = destination;
            this.IsCommand = this.Destination.chBit;
            this.Digis = digis;
            this.Length = 14 + ( 7 * digis.Length );
		}

        /// <summary>
        /// Build the header from a template header.
        /// </summary>
        /// <param name="template">Template header.</param>
        /// <param name="command">Command bit.</param>
        /// <param name="response">Response bit.</param>
        public AX25Header(AX25Header template, bool command, bool response)
        {
            this.Source = new L2Callsign(template.Source, response);
            this.IsResponse = response;
            this.Destination = new L2Callsign(template.Destination, command);
            this.IsCommand = command;
            this.Digis = template.Digis;
            this.Length = template.Length;
        }
		
		/// <summary>
		/// The source of the frame (where it comes from).
		/// </summary>
		public readonly L2Callsign Source;
		
		/// <summary>
		/// The destination of the frame (where it should arrive).
		/// </summary>
		public readonly L2Callsign Destination;
		
		/// <summary>
		/// The intermediate digis on the path.
		/// </summary>
		public readonly L2Callsign[] Digis;

        /// <summary>
        /// C-Bit of the destination callsign.
        /// </summary>
        public readonly bool IsCommand;

        /// <summary>
        /// C-Bit of the source callsign.
        /// </summary>
        public readonly bool IsResponse;

        /// <summary>
        /// Length of header data [octets].
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// Test if this frame is a V1 frame.
        /// </summary>
        public Boolean IsV1Frame
        {
            get
            {
                return (IsCommand == IsResponse);
            }
        }

        /// <summary>
        /// Test if this frame is a V2 frame.
        /// </summary>
        public Boolean IsV2Frame
        {
            get
            {
                return (IsCommand != IsResponse);
            }
        }

        /// <summary>
		/// The next station to go.
		/// </summary>
		public L2Callsign NextHop
		{
			get {
				foreach (L2Callsign cs in this.Digis)
					if (!cs.chBit)
						return cs;
				return this.Destination;
			}
		}

        /// <summary>
        /// Check if the frame has passed all digipeaters, if any.
        /// </summary>
        /// <returns>If all digipeaters are passed.</returns>
        public bool MustDigi()
        {
            foreach (L2Callsign digi in Digis)
                if (!digi.chBit)
                    return true;
            return false;
        }

        /// <summary>
        /// Do hop.
        /// </summary>
        /// <returns></returns>
        public void Digipeat()
        {
            for (int i = 0; i < Digis.Length; ++i)
            {
                L2Callsign hop = Digis[i];
                if (!hop.chBit)
                {
                    Digis[i] = new L2Callsign(hop, true);
                    return;
                }
            } // end for //
            throw new Exception("All digis are already passed");
        }

        /// <summary>
        /// Get binary presentation of this header.
        /// </summary>
        public byte[] Header
        {
            get
            {
                int nDigis = Digis.Length;
                byte[] frame = new byte[14 + ( nDigis * 7 )];
                SetHeader(frame);
                return frame;
            }
        }

        /// <summary>
        /// Fill in frame with header data.
        /// </summary>
        /// <param name="frame">The frame to set the data to.</param>
        public void SetHeader(byte[] frame)
        {
            if (frame == null)
                throw new ArgumentNullException("frame");
            int nDigis = Digis.Length;
            if (frame.Length < 14 + ( nDigis * 7 ))
                throw new Exception("frame is too short");
            Array.Copy(Destination.Octets, 0, frame, 0, 7);
            Array.Copy(Source.Octets, 0, frame, 7, 7);
            int i = 14;
            for (int iDigi = 0; iDigi < nDigis; ++iDigi, i += 7)
                Array.Copy(Digis[iDigi].Octets, 0, frame, i, 7);
            frame[i - 1] |= 0x01; // SDLC end bit
        }

		/// <summary>
		/// Get a string representation of this header.
		/// </summary>
		/// <returns>String representation of this header.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(this.Source.ToString());
			sb.Append(" -> ");
			sb.Append(this.Destination.ToString());
			if (this.Digis.Length > 0) {
				sb.Append(" via");
				foreach (L2Callsign cs in this.Digis) {
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
