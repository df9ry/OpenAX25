using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public abstract class AX25Frame
    {

        protected          byte[]     m_octets;
        protected readonly AX25Modulo m_modulo;

        protected AX25Frame(byte[] octets, AX25Modulo modulo)
        {
            m_octets = octets;
            m_modulo = modulo;
        }

        public static AX25Frame Create(byte[] octets, AX25Modulo modulo = AX25Modulo.UNSPECIFIED)
        {
            if (octets == null)
                throw new ArgumentNullException("octets");
            if (octets.Length == 0)
                return new AX25InvalidFrame(octets);
            if ((octets[0] & 0x01) == 0x00) // I-Frame:
                return new AX25_I(octets, modulo);
            if ((octets[0] & 0x02) == 0x00) // S-Frame:
                return AX25SFrame.Create(octets, modulo);
            else
                return AX25UFrame.Create(octets);
        }

        protected static int Size(AX25Modulo modulo)
        {
            switch (modulo)
            {
                case AX25Modulo.UNSPECIFIED: return 0;
                case AX25Modulo.MOD8: return 1;
                case AX25Modulo.MOD128: return 2;
                default: throw new ArgumentOutOfRangeException("modulo");
            } // end switch //
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }

        protected virtual void ToString(StringBuilder sb)
        {
        }
    }

}
