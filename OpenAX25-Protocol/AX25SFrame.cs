using OpenAX25Contracts;
using System;

namespace OpenAX25_Protocol
{
    public abstract class AX25SFrame : AX25Frame
    {
        protected AX25SFrame(byte[] octets, AX25Modulo modulo, bool cmd, bool rsp)
            : base(octets, modulo, cmd, rsp)
        {
        }

        internal static AX25Frame Create(byte[] octets, AX25Modulo modulo, bool cmd, bool rsp)
        {
            switch (((modulo != AX25Modulo.MOD128)?(octets[0] & 0x0C):octets[0]))
            {
                case 0x00: return new AX25_RR(octets, modulo, cmd, rsp);
                case 0x04: return new AX25_RNR(octets, modulo, cmd, rsp);
                case 0x08: return new AX25_REJ(octets, modulo, cmd, rsp);
                case 0x0C: return new AX25_SREJ(octets, modulo, cmd, rsp);
                default: return new AX25InvalidFrame(octets, cmd, rsp);
            } // end switch //
        }

        public bool PF
        {
            get
            {
                switch (m_modulo)
                {
                    case AX25Modulo.MOD8: return ((m_octets[0] & 0x10) != 0x00);
                    case AX25Modulo.MOD128: return ((m_octets[1] & 0x01) != 0x10);
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
                        throw new Exception("Attempt to sett PF with unknown modulo");
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
                    default:
                        throw new Exception("Attempt to get N_R with unknown modulo");
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
                        throw new Exception("Attempt to set N_R with unknown modulo");
                } // end switch //
            }
        }

    }
}
