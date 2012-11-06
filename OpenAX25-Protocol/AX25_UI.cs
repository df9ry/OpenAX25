﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenAX25Core;

namespace OpenAX25_Protocol
{
    public class AX25_UI : AX25UFrame
    {

        public AX25_UI(byte[] i, bool pf, bool cmd = true, bool rsp = false)
            : base(new byte[1 + i.Length], cmd, rsp)
        {
            m_octets[0] = 0x03;
            I   = i;
            PF  = pf;
        }

        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.UI;
            }
        }

        internal AX25_UI(byte[] octets, bool cmd, bool rsp)
            : base(octets, cmd, rsp)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("UI");
            if (PF)
                sb.Append("(P/F)");
            sb.Append(' ');
            sb.Append(HexConverter.ToHexString(I, true));
        }
    }
}
