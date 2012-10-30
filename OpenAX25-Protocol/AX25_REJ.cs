using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_REJ : AX25SFrame
    {

        public AX25_REJ(AX25Modulo modulo, int n_r, bool pf)
            : base(new byte[Size(modulo)], modulo)
        {
            m_octets[0] = 0x09;
            N_R = n_r;
            PF  = pf;
        }

        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.REJ;
            }
        }

        internal AX25_REJ(byte[] octets, AX25Modulo modulo)
            : base(octets, modulo)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("REJ(R=");
            sb.Append(N_R);
            if (PF)
                sb.Append(",F");
            else
                sb.Append(",P");
            sb.Append(')');
        }
    }
}
