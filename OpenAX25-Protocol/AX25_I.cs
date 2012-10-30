using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenAX25Core;

namespace OpenAX25_Protocol
{
    public sealed class AX25_I : AX25Frame
    {

        public AX25_I(byte[] i, AX25Modulo modulo, int n_r, int n_s, bool p)
            : base(new byte[Size(modulo) + i.Length], modulo)
        {
            m_octets[0] = 0x00;
            I = i;
            N_R = n_r;
            N_S = n_s;
            P = p;
        }

        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.I;
            }
        }

        internal AX25_I(byte[] octets, AX25Modulo modulo)
            : base(octets, modulo)
        {
        }

        public bool P
        {
            get
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8: return ((m_octets[0] & 0x10) != 0x00);
                    case AX25Modulo.MOD128: return ((m_octets[1] & 0x01) != 0x10);
                    default: return false;
                } // end switch //
            }
            set
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8:
                        if (value)
                            m_octets[0] = (byte)(m_octets[0] | 0x10);
                        else
                            m_octets[0] = (byte)(m_octets[0] & 0xEF);
                        break;
                    case AX25Modulo.MOD128:
                        if (value)
                            m_octets[1] = (byte)(m_octets[1] | 0x01);
                        else
                            m_octets[1] = (byte)(m_octets[1] & 0xFE);
                        break;
                    default:
                        break;
                } // end switch //
            }
        }

        public int N_R
        {
            get
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8: return (m_octets[0] & 0xE0) >> 5;
                    case AX25Modulo.MOD128: return (m_octets[1] & 0xFE) >> 1;
                    default: return -1;
                } // end switch //
            }
            set
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8:
                        m_octets[0] = (byte)(m_octets[0] & 0x1F);
                        m_octets[0] |= (byte)((value & 0x07) << 5);
                        break;
                    case AX25Modulo.MOD128:
                        m_octets[1] = (byte)(m_octets[1] & 0x01);
                        m_octets[1] |= (byte)((value & 0x7F) << 1);
                        break;
                    default:
                        break;
                } // end switch //
            }
        }

        public int N_S
        {
            get
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8: return (m_octets[0] & 0x0E) >> 1;
                    case AX25Modulo.MOD128: return (m_octets[0] & 0xFE) >> 1;
                    default: return -1;
                } // end switch //
            }
            set
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8:
                        m_octets[0] = (byte)(m_octets[0] & 0xF1);
                        m_octets[0] |= (byte)((value & 0x07) << 1);
                        break;
                    case AX25Modulo.MOD128:
                        m_octets[0] = (byte)(m_octets[0] & 0x01);
                        m_octets[0] |= (byte)((value & 0x7F) << 1);
                        break;
                    default:
                        break;
                } // end switch //
            }
        }

        public byte[] I
        {
            get
            {
                byte[] value;
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8:
                        value = new byte[m_octets.Length - 1];
                        Array.Copy(m_octets, 1, value, 0, value.Length);
                        break;
                    case AX25Modulo.MOD128:
                        value = new byte[m_octets.Length - 2];
                        Array.Copy(m_octets, 2, value, 0, value.Length);
                        break;
                    default:
                        value = new byte[m_octets.Length];
                        Array.Copy(m_octets, 0, value, 0, value.Length);
                        break;
                } // end switch //
                return value;
            }
            set
            {
                int s_hdr = Size(m_modulo);
                int s_dat = value.Length;
                int s_tot = s_hdr + s_dat;
                if (s_tot != m_octets.Length)
                {
                    byte[] octets = new byte[s_tot];
                    Array.Copy(m_octets, 0, octets, 0, s_hdr);
                    m_octets = octets;
                }
                Array.Copy(value, 0, m_octets, s_hdr, s_dat);
            }
        }

        public int InfoFieldLength
        {
            get
            {
                return m_octets.Length - Size(m_modulo);
            }
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("I(R=");
            sb.Append(N_R);
            sb.Append(",S=");
            sb.Append(N_S);
            if (P)
                sb.Append(",P");
            sb.Append(") ");
            sb.Append(L2HexConverter.ToHexString(I, true));
        }
    }
}
