using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_SABM : AX25UFrame
    {

        public AX25_SABM(bool p, bool cmd = true, bool rsp = false)
            : base(new byte[1], cmd, rsp)
        {
            m_octets[0] = 0x2F;
            PF  = p;
        }

        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.SABM;
            }
        }

        internal AX25_SABM(byte[] octets, bool cmd, bool rsp)
            : base(octets, cmd, rsp)
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
