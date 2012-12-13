//
// AX25_XID.cs
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
using System;

namespace OpenAX25Contracts
{

    /// <summary>
    /// AX.25 XID Frame.
    /// </summary>
    public sealed class AX25_XID : AX25U
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="i">Information field.</param>
        /// <param name="pf">Poll / Final bit.</param>
        /// <param name="cmd">Command?</param>
        /// <param name="rsp">Response?</param>
        public AX25_XID(byte[] i, bool pf, bool cmd = true, bool rsp = false)
            : base(new byte[1 + i.Length], cmd, rsp)
        {
            m_payload[0] = 0xAF;
            I   = i;
            PF  = pf;
        }

        /// <summary>
        /// Get Frame Type.
        /// </summary>
        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T.XID;
            }
        }

        internal AX25_XID(byte[] octets, bool cmd, bool rsp)
            : base(octets, cmd, rsp)
        {
        }

        /// <summary>
        /// ToString method.
        /// </summary>
        /// <param name="sb">String builder.</param>
        protected override void ToString(StringBuilder sb)
        {
            sb.Append("XID");
            if (PF)
            {
                if (Command)
                    sb.Append("(P)");
                else
                    sb.Append("(F)");
            }
            sb.Append(' ');
            sb.Append(String.Format("{0} octets", I.Length));
        }
    }
}
