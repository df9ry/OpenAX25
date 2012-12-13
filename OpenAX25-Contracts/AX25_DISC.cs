//
// AX25_DISC.cs
// 
//  Author:
//       Tania Knoebl (DF9RY) DF9RY@DARC.de
//  
//  Copyright © 2012 Tania Knoebl (DF9RY)
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>
//

using System.Text;

namespace OpenAX25Contracts
{

    /// <summary>
    /// AX.25 DISC Frame.
    /// </summary>
    public sealed class AX25_DISC : AX25U
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="p">Poll?</param>
        /// <param name="cmd">Command?</param>
        /// <param name="rsp">Response?</param>
        public AX25_DISC(bool p, bool cmd = true, bool rsp = false)
            : base(new byte[1], cmd, rsp)
        {
            m_payload[0] = 0x43;
            PF  = p;
        }

        /// <summary>
        /// Get Frame Type.
        /// </summary>
        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.DISC;
            }
        }

        internal AX25_DISC(byte[] frame, bool cmd, bool rsp)
            : base(frame, cmd, rsp)
        {
        }

        /// <summary>
        /// To String method.
        /// </summary>
        /// <param name="sb">StringBuilder.</param>
        protected override void ToString(StringBuilder sb)
        {
            sb.Append("DISC");
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
