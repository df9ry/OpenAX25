using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenAX25Core;

namespace OpenAX25_Protocol
{
    public class AX25_UI : AX25UFrame
    {

        public AX25_UI(byte[] i, bool pf)
            : base(new byte[1 + i.Length])
        {
            m_octets[0] = 0x03;
            I   = i;
            PF  = pf;
        }

        internal AX25_UI(byte[] octets)
            : base(octets)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("UI");
            if (PF)
                sb.Append("(P/F)");
            sb.Append(' ');
            sb.Append(L2HexConverter.ToHexString(I, true));
        }
    }
}
