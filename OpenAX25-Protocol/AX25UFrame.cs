using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public abstract class AX25UFrame : AX25Frame
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

        protected AX25UFrame(byte[] octets, bool cmd, bool rsp)
            : base(octets, 0, cmd, rsp)
        {
        }

        internal static AX25Frame Create(byte[] octets, bool cmd, bool rsp)
        {
            switch (octets[0] & PF_NOTM)
            {
                case SABME: return new AX25_SABME(octets, cmd, rsp);
                case SABM : return new AX25_SABM(octets, cmd, rsp);
                case DISC : return new AX25_DISC(octets, cmd, rsp);
                case DM   : return new AX25_DM(octets, cmd, rsp);
                case UA   : return new AX25_UA(octets, cmd, rsp);
                case FRMR : return new AX25_FRMR(octets, cmd, rsp);
                case UI   : return new AX25_UI(octets, cmd, rsp);
                case XID  : return new AX25_XID(octets, cmd, rsp);
                case TEST : return new AX25_TEST(octets, cmd, rsp);
                default: return new AX25InvalidFrame(octets, cmd, rsp);
            } // end switch //
        }

        public bool PF
        {
            get
            {
                return ((m_octets[0] & PF_MASK) != 0x00);
            }
            set
            {
                if (value)
                    m_octets[0] = (byte)(m_octets[0] | PF_MASK);
                else
                    m_octets[0] = (byte)(m_octets[0] & PF_NOTM);
            }
        }

        public byte[] I
        {
            get
            {
                byte[] value = new byte[m_octets.Length - 1];
                Array.Copy(m_octets, 1, value, 0, value.Length);
                return value;
            }
            set
            {
                int s_dat = value.Length;
                int s_tot = 1 + s_dat;
                if (s_tot != m_octets.Length)
                {
                    byte[] octets = new byte[s_tot];
                    octets[0] = m_octets[0];
                    m_octets = octets;
                }
                Array.Copy(value, 0, m_octets, 1, s_dat);
            }
        }

        public int InfoFieldLength
        {
            get
            {
                return m_octets.Length - 1;
            }
        }

    }
}
