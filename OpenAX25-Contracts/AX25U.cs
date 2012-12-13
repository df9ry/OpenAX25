//
// AX25U.cs
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
    /// AX.25 Unnumbered Frame.
    /// </summary>
    public abstract class AX25U : AX25Payload
    {

        private const byte SABME = 0x6F;
        private const byte SABM  = 0x2F;
        private const byte DISC  = 0x43;
        private const byte DM    = 0x0F;
        private const byte UA    = 0x63;
        private const byte FRMR  = 0x87;
        private const byte UI    = 0x03;
        private const byte XID   = 0xAF;
        private const byte TEST  = 0xE3;

        private const byte PF_MASK = 0x10;
        private const byte PF_NOTM = 0xEF;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="payload">Information field</param>
        /// <param name="cmd">Command?</param>
        /// <param name="rsp">Response?</param>
        protected AX25U(byte[] payload, bool cmd, bool rsp)
            : base(payload, 0, cmd, rsp)
        {
        }

        internal static AX25Payload Create(byte[] frame, bool cmd, bool rsp)
        {
            switch (frame[0] & PF_NOTM)
            {
                case SABME: return new AX25_SABME(frame, cmd, rsp);
                case SABM : return new AX25_SABM(frame, cmd, rsp);
                case DISC : return new AX25_DISC(frame, cmd, rsp);
                case DM   : return new AX25_DM(frame, cmd, rsp);
                case UA   : return new AX25_UA(frame, cmd, rsp);
                case FRMR : return new AX25_FRMR(frame, cmd, rsp);
                case UI   : return new AX25_UI(frame, cmd, rsp);
                case XID  : return new AX25_XID(frame, cmd, rsp);
                case TEST : return new AX25_TEST(frame, cmd, rsp);
                default: return new AX25InvalidFrame(frame, cmd, rsp);
            } // end switch //
        }

        /// <summary>
        /// Poll / Final bit.
        /// </summary>
        public bool PF
        {
            get
            {
                return ((m_payload[0] & PF_MASK) != 0x00);
            }
            set
            {
                if (value)
                    m_payload[0] = (byte)(m_payload[0] | PF_MASK);
                else
                    m_payload[0] = (byte)(m_payload[0] & PF_NOTM);
            }
        }

        /// <summary>
        /// Information field.
        /// </summary>
        public byte[] I
        {
            get
            {
                byte[] value = new byte[m_payload.Length - 1];
                Array.Copy(m_payload, 1, value, 0, value.Length);
                return value;
            }
            set
            {
                int s_dat = value.Length;
                int s_tot = 1 + s_dat;
                if (s_tot != m_payload.Length)
                {
                    byte[] frame = new byte[s_tot];
                    frame[0] = m_payload[0];
                    m_payload = frame;
                }
                Array.Copy(value, 0, m_payload, 1, s_dat);
            }
        }

        /// <summary>
        /// Info field length.
        /// </summary>
        public int InfoFieldLength
        {
            get
            {
                return m_payload.Length - 1;
            }
        }

    }
}
