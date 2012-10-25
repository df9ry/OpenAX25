using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_DM : AX25UFrame
    {

        public AX25_DM(bool f)
            : base(new byte[1])
        {
            m_octets[0] = 0x0F;
            PF  = f;
        }

        internal AX25_DM(byte[] octets)
            : base(octets)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("DM");
            if (PF)
                sb.Append("(F)");
        }

    }
}
