//
// AX25S.cs
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
    /// AX.25 Supervisory Frame.
    /// </summary>
    public abstract class AX25S : AX25Payload
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="payload">Data bytes.</param>
        /// <param name="modulo">Modulo.</param>
        /// <param name="cmd">Command?</param>
        /// <param name="rsp">Respose?</param>
        protected AX25S(byte[] payload, AX25Modulo modulo, bool cmd, bool rsp)
            : base(payload, modulo, cmd, rsp)
        {
        }

        internal static AX25Payload Create(byte[] frame, AX25Modulo modulo, bool cmd, bool rsp)
        {
            switch (((modulo != AX25Modulo.MOD128)?(frame[0] & 0x0C):frame[0]))
            {
                case 0x00: return new AX25_RR(frame, modulo, cmd, rsp);
                case 0x04: return new AX25_RNR(frame, modulo, cmd, rsp);
                case 0x08: return new AX25_REJ(frame, modulo, cmd, rsp);
                case 0x0C: return new AX25_SREJ(frame, modulo, cmd, rsp);
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
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8: return ((m_payload[0] & 0x10) != 0x00);
                    case AX25Modulo.MOD128: return ((m_payload[1] & 0x01) != 0x10);
                    default:
                        throw new Exception("Attempt to get PF with unknown modulo");
                } // end switch //
            }
            set
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8:
                        if (value)
                            m_payload[0] = (byte)(m_payload[0] | 0x10);
                        else
                            m_payload[0] = (byte)(m_payload[0] & 0xEF);
                        break;
                    case AX25Modulo.MOD128:
                        if (value)
                            m_payload[1] = (byte)(m_payload[1] | 0x01);
                        else
                            m_payload[1] = (byte)(m_payload[1] & 0xFE);
                        break;
                    default:
                        throw new Exception("Attempt to sett PF with unknown modulo");
                } // end switch //
            }
        }

        /// <summary>
        /// N(r).
        /// </summary>
        public int N_R
        {
            get
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8: return (m_payload[0] & 0xE0) >> 5;
                    case AX25Modulo.MOD128: return (m_payload[1] & 0xFE) >> 1;
                    default:
                        throw new Exception("Attempt to get N_R with unknown modulo");
                } // end switch //
            }
            set
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8:
                        m_payload[0] = (byte)(m_payload[0] & 0x1F);
                        m_payload[0] |= (byte)((value & 0x07) << 5);
                        break;
                    case AX25Modulo.MOD128:
                        m_payload[1] = (byte)(m_payload[1] & 0x01);
                        m_payload[1] |= (byte)((value & 0x7F) << 1);
                        break;
                    default:
                        throw new Exception("Attempt to set N_R with unknown modulo");
                } // end switch //
            }
        }

    }
}
