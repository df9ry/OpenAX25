using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_FRMR : AX25UFrame
    {

        public AX25_FRMR(bool f)
            : base(new byte[1])
        {
            m_octets[0] = 0x87;
            PF  = f;
        }
    
        internal AX25_FRMR(byte[] octets)
            : base(octets)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("FRMR");
            if (PF)
                sb.Append("(F)");
        }
    }
}
