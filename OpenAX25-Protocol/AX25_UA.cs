using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_UA : AX25UFrame
    {

        public AX25_UA(bool f)
            : base(new byte[1])
        {
            m_octets[0] = 0x63;
            PF  = f;
        }

        internal AX25_UA(byte[] octets)
            : base(octets)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("UA");
            if (PF)
                sb.Append("(F)");
        }

    }
}
