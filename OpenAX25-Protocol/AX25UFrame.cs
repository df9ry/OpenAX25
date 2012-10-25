using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public abstract class AX25UFrame : AX25Frame
    {

        protected AX25UFrame(byte[] i, bool pf)
            : base(new byte[i.Length + 1], 0)
        {
            I = i;
            PF = pf;
        }

        protected AX25UFrame(bool pf)
            : base(new byte[0], 0)
        {
            PF = pf;
        }

        protected AX25UFrame(byte[] octets)
            : base(octets, 0)
        {
        }

        internal static AX25Frame Create(byte[] octets)
        {
            switch (octets[0] & 0xEC)
            {
                case 0x6C: return new AX25_SABME(octets);
                case 0x2C: return new AX25_SABM(octets);
                case 0x40: return new AX25_DISC(octets);
                case 0x0C: return new AX25_DM(octets);
                case 0x60: return new AX25_UA(octets);
                case 0x81: return new AX25_FRMR(octets);
                case 0x00: return new AX25_UI(octets);
                case 0x9C: return new AX25_XID(octets);
                case 0xE0: return new AX25_TEST(octets);
                default: return new AX25InvalidFrame(octets);
            } // end switch //
        }

        public bool PF
        {
            get
            {
                return ((m_octets[0] & 0x10) != 0x00);
            }
            set
            {
                if (value)
                    m_octets[0] = (byte)(m_octets[0] | 0x10);
                else
                    m_octets[0] = (byte)(m_octets[0] & 0xEF);
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

    }
}
