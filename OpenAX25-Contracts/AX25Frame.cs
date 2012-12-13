//
// AX25Frame.cs
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

namespace OpenAX25Contracts
{
    /// <summary>
    /// A complete AX.25 Frame, fully decoded.
    /// </summary>
    public sealed class AX25Frame
    {
        /// <summary>
        /// Frame heaader.
        /// </summary>
        public readonly AX25Header  Header;

        /// <summary>
        /// Frame payload.
        /// </summary>
        public readonly AX25Payload Payload;

        private byte[] m_octets = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="frame">Frame octets.</param>
        /// <param name="modulo">AX.25 Modulo</param>
        public AX25Frame(byte[] frame, AX25Modulo modulo)
        {
            if (frame == null)
                throw new ArgumentNullException("frame");
            Header = new AX25Header(frame);
            Payload = AX25Payload.Create(frame, Header.Length,
                Header.IsCommand, Header.IsResponse, modulo);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="header">AX.25 header.</param>
        /// <param name="payload">Paylaod data.</param>
        /// <param name="modulo">AX.25 Modulo</param>
        public AX25Frame(AX25Header header, byte[] payload, AX25Modulo modulo)
        {
            if (payload == null)
                throw new ArgumentNullException("payload");
            Payload = AX25Payload.Create(payload,
                Header.IsCommand, Header.IsResponse, modulo);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="header">AX.25 header.</param>
        /// <param name="payload">AX.25 payload</param>
        public AX25Frame(AX25Header header, AX25Payload payload)
        {
            if (payload == null)
                throw new ArgumentNullException("payload");
            Header = new AX25Header(header, payload.Command, payload.Response);
            Payload = payload;
        }

        /// <summary>
        /// Command?
        /// </summary>
        public bool IsCommand
        {
            get
            {
                return Header.IsCommand;
            }
        }

        /// <summary>
        /// Response?
        /// </summary>
        public bool IsResponse
        {
            get
            {
                return Header.IsResponse;
            }
        }

        /// <summary>
        /// Old version 1.x frame?
        /// </summary>
        public bool IsV1Frame
        {
            get
            {
                return Header.IsV1Frame;
            }
        }

        /// <summary>
        /// Recent version 2.x frame?
        /// </summary>
        public bool IsV2Frame
        {
            get
            {
                return Header.IsV2Frame;
            }
        }

        /// <summary>
        /// Frame octets.
        /// </summary>
        public byte[] Octets
        {
            get
            {
                if (m_octets == null)
                {
                    lock (this)
                    {
                        if (m_octets == null)
                        {
                            byte[] payload = Payload.Octets;
                            int lHeader = Header.Length;
                            int lPayload = payload.Length;
                            int lFrame = lHeader + lPayload;
                            m_octets = new byte[lFrame];
                            Header.SetHeader(m_octets);
                            Array.Copy(payload, 0, m_octets, lHeader, lPayload);
                        }
                    } // end lock //
                }
                return m_octets;
            }
        }

        /// <summary>
        /// The ToString() method.
        /// </summary>
        /// <returns>Human readable representation of this object.</returns>
        public override string ToString()
        {
            return Header.ToString() + " " + Payload.ToString();
        }

    }
}
