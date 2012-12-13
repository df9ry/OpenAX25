//
// AX25_DM.cs
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
    /// AX.25 DM Frame.
    /// </summary>
    public sealed class AX25_DM : AX25U
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="f">Final?</param>
        /// <param name="cmd">Command?</param>
        /// <param name="rsp">Response?</param>
        public AX25_DM(bool f, bool cmd = true, bool rsp = false)
            : base(new byte[1], cmd, rsp)
        {
            m_payload[0] = 0x0F;
            PF  = f;
        }

        /// <summary>
        /// Get Frame Type.
        /// </summary>
        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.DM;
            }
        }

        internal AX25_DM(byte[] octets, bool cmd, bool rsp)
            : base(octets, cmd, rsp)
        {
        }

        /// <summary>
        /// To String method.
        /// </summary>
        /// <param name="sb">StringBuilder.</param>
        protected override void ToString(StringBuilder sb)
        {
            sb.Append("DM");
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
