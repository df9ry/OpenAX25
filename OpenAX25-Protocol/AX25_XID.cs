using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenAX25Core;

namespace OpenAX25_Protocol
{
    public class AX25_XID : AX25UFrame
    {

        public AX25_XID(byte[] i, bool pf, bool cmd = true, bool rsp = false)
            : base(new byte[1 + i.Length], cmd, rsp)
        {
            m_octets[0] = 0xAF;
            I   = i;
            PF  = pf;
        }

        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.XID;
            }
        }

        internal AX25_XID(byte[] octets, bool cmd, bool rsp)
            : base(octets, cmd, rsp)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("XID");
            if (PF)
            {
                if (Command)
                    sb.Append("(P)");
                else
                    sb.Append("(F)");
            }
            sb.Append(' ');
            sb.Append(HexConverter.ToHexString(I, true));
        }
    }
}
