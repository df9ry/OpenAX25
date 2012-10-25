using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{
    public class AX25_SREJ : AX25SFrame
    {

        public AX25_SREJ(AX25Modulo modulo, int n_r, bool pf)
            : base(new byte[Size(modulo)], modulo)
        {
            m_octets[0] = 0x0D;
            N_R = n_r;
            PF  = pf;
        }
    
        internal AX25_SREJ(byte[] octets, AX25Modulo modulo)
            : base(octets, modulo)
        {
        }

        protected override void ToString(StringBuilder sb)
        {
            sb.Append("SREJ(R=");
            sb.Append(N_R);
            if (PF)
                sb.Append(",F");
            else
                sb.Append(",P");
            sb.Append(')');
        }
    }
}
