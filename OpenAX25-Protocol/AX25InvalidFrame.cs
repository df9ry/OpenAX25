using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenAX25Core;

namespace OpenAX25_Protocol
{
    public sealed class AX25InvalidFrame : AX25Frame
    {

        internal AX25InvalidFrame(byte[] octets, bool cmd, bool rsp)
            : base(octets, 0, cmd, rsp)
        {
        }


        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T._INV;
            }
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("IFRM ");
            sb.Append(HexConverter.ToHexString(m_octets, true));
        }
    }
}
