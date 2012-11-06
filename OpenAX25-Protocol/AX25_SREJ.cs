using System.Text;
using OpenAX25Contracts;

namespace OpenAX25_Protocol
{
    public class AX25_SREJ : AX25SFrame
    {

        public AX25_SREJ(AX25Modulo modulo, int n_r, bool pf, bool cmd = true, bool rsp = false)
            : base(new byte[Size(modulo)], modulo, cmd, rsp)
        {
            m_octets[0] = 0x0D;
            N_R = n_r;
            PF  = pf;
        }

        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.SREJ;
            }
        }

        internal AX25_SREJ(byte[] octets, AX25Modulo modulo, bool cmd, bool rsp)
            : base(octets, modulo, cmd, rsp)
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
