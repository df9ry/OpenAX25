using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_DISC : AX25UFrame
    {

        public AX25_DISC(bool p)
            : base(new byte[1])
        {
            m_octets[0] = 0x43;
            PF  = p;
        }

        public override AX25Frame_T FrameType {
            get
            {
                return AX25Frame_T.DISC;
            }
        }

        internal AX25_DISC(byte[] octets)
            : base(octets)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("DISC");
            if (PF)
                sb.Append("(P)");
        }

    }
}
