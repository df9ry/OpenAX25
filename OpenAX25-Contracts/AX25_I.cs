//
// AX25_I.cs
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
    /// AX.25 I Frame.
    /// </summary>
    public sealed class AX25_I : AX25Payload
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="i">Information field.</param>
        /// <param name="modulo">Modulo.</param>
        /// <param name="n_r">N(r).</param>
        /// <param name="n_s">N(s).</param>
        /// <param name="p">Poll?</param>
        /// <param name="cmd">Command?</param>
        /// <param name="rsp">Response?</param>
        public AX25_I(byte[] i, AX25Modulo modulo, int n_r, int n_s, bool p, bool cmd = true, bool rsp = false)
            : base(new byte[ModuloSize(modulo) + i.Length], modulo, cmd, rsp)
        {
            m_payload[0] = 0x00;
            I = i;
            N_R = n_r;
            N_S = n_s;
            P = p;
        }

        /// <summary>
        /// Get Frame Type.
        /// </summary>
        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.I;
            }
        }

        internal AX25_I(byte[] octets, AX25Modulo modulo, bool cmd, bool rsp)
            : base(octets, modulo, cmd, rsp)
        {
        }

        /// <summary>
        /// Poll bit.
        /// </summary>
        public bool P
        {
            get
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8: return ((m_payload[0] & 0x10) != 0x00);
                    case AX25Modulo.MOD128: return ((m_payload[1] & 0x01) != 0x10);
                    default:
                        throw new Exception("Attempt to get P bit with unknown modulo");
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
                        throw new Exception("Attempt to set P bit with unknown modulo");
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

        /// <summary>
        /// N(s).
        /// </summary>
        public int N_S
        {
            get
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8: return (m_payload[0] & 0x0E) >> 1;
                    case AX25Modulo.MOD128: return (m_payload[0] & 0xFE) >> 1;
                    default:
                        throw new Exception("Attempt to get N_S with unknown modulo");
                } // end switch //
            }
            set
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8:
                        m_payload[0] = (byte)(m_payload[0] & 0xF1);
                        m_payload[0] |= (byte)((value & 0x07) << 1);
                        break;
                    case AX25Modulo.MOD128:
                        m_payload[0] = (byte)(m_payload[0] & 0x01);
                        m_payload[0] |= (byte)((value & 0x7F) << 1);
                        break;
                    default:
                        throw new Exception("Attempt to set N_S with unknown modulo");
                } // end switch //
            }
        }

        /// <summary>
        /// Information field.
        /// </summary>
        public byte[] I
        {
            get
            {
                byte[] value;
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8:
                        value = new byte[m_payload.Length - 1];
                        Array.Copy(m_payload, 1, value, 0, value.Length);
                        break;
                    case AX25Modulo.MOD128:
                        value = new byte[m_payload.Length - 2];
                        Array.Copy(m_payload, 2, value, 0, value.Length);
                        break;
                    default:
                        value = new byte[m_payload.Length];
                        Array.Copy(m_payload, 0, value, 0, value.Length);
                        break;
                } // end switch //
                return value;
            }
            set
            {
                int s_hdr = ModuloSize(m_modulo);
                int s_dat = value.Length;
                int s_tot = s_hdr + s_dat;
                if (s_tot != m_payload.Length)
                {
                    byte[] octets = new byte[s_tot];
                    Array.Copy(m_payload, 0, octets, 0, s_hdr);
                    m_payload = octets;
                }
                Array.Copy(value, 0, m_payload, s_hdr, s_dat);
            }
        }

        /// <summary>
        /// Length of the info field.
        /// </summary>
        public int InfoFieldLength
        {
            get
            {
                return m_payload.Length - ModuloSize(m_modulo);
            }
        }

        /// <summary>
        /// To String method.
        /// </summary>
        /// <param name="sb">StringBuilder.</param>
        protected override void ToString(StringBuilder sb)
        {
            sb.Append("I(R=");
            sb.Append(N_R);
            sb.Append(",S=");
            sb.Append(N_S);
            if (P)
                sb.Append(",P");
            sb.Append(") ");
            sb.Append(String.Format("{0} octets", I.Length));
        }
    }
}
