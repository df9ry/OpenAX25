using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_DM : AX25UFrame
    {

        public AX25_DM(bool f, bool cmd = true, bool rsp = false)
            : base(new byte[1], cmd, rsp)
        {
            m_octets[0] = 0x0F;
            PF  = f;
        }

        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.DM;
            }
        }

        internal AX25_DM(byte[] octets, bool cmd, bool rsp)
            : base(octets, cmd, rsp)
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
