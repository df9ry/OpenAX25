using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_DISC : AX25UFrame
    {

        public AX25_DISC(bool p, bool cmd = true, bool rsp = false)
            : base(new byte[1], cmd, rsp)
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

        internal AX25_DISC(byte[] octets, bool cmd, bool rsp)
            : base(octets, cmd, rsp)
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
