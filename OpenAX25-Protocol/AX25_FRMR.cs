using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_FRMR : AX25UFrame
    {

        public AX25_FRMR(bool f, bool cmd = true, bool rsp = false)
            : base(new byte[1], cmd, rsp)
        {
            m_octets[0] = 0x87;
            PF  = f;
        }

        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.FRMR;
            }
        }

        internal AX25_FRMR(byte[] octets, bool cmd, bool rsp)
            : base(octets, cmd, rsp)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("FRMR");
            if (PF)
            {
                if (Command)
                    sb.Append("(P)");
                else
                    sb.Append("(F)");
            }
        }
    }
}
