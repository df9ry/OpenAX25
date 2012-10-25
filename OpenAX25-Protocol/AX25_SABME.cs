﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_SABME : AX25UFrame
    {

        public AX25_SABME(bool p)
            : base(new byte[1])
        {
            m_octets[0] = 0x6F;
            PF  = p;
        }

        internal AX25_SABME(byte[] octets)
            : base(octets)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("SABME");
            if (PF)
                sb.Append("(P)");
        }
    }
}