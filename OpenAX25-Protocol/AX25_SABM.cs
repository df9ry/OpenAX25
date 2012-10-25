using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_SABM : AX25UFrame
    {

        public AX25_SABM(bool p)
            : base(new byte[1])
        {
            m_octets[0] = 0x2F;
            PF  = p;
        }
    
        internal AX25_SABM(byte[] octets)
            : base(octets)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("SABM");
            if (PF)
                sb.Append("(P)");
        }
    }
}
