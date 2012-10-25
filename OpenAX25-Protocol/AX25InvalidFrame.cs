using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenAX25Core;

namespace OpenAX25_Protocol
{
    public sealed class AX25InvalidFrame : AX25Frame
    {

        internal AX25InvalidFrame(byte[] octets)
            : base(octets, 0)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("IFRM ");
            sb.Append(L2HexConverter.ToHexString(m_octets, true));
        }
    }
}
