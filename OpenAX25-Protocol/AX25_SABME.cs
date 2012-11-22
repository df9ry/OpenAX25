using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_SABME : AX25UFrame
    {

        public AX25_SABME(bool p, bool cmd = true, bool rsp = false)
            : base(new byte[1], cmd, rsp)
        {
            m_octets[0] = 0x6F;
            PF  = p;
        }

        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.SABME;
            }
        }

        internal AX25_SABME(byte[] octets, bool cmd, bool rsp)
            : base(octets, cmd, rsp)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("SABME");
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
