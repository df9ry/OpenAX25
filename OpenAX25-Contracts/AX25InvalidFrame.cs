//
// AX25InvalidFrame.cs
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
    /// AX.25 Invalid Frame.
    /// </summary>
    public sealed class AX25InvalidFrame : AX25Payload
    {

        internal AX25InvalidFrame(byte[] octets, bool cmd, bool rsp)
            : base(octets, 0, cmd, rsp)
        {
        }

        /// <summary>
        /// Get Frame Type.
        /// </summary>
        public override AX25Frame_T FrameType
        {
            get
            {
                return AX25Frame_T._INV;
            }
        }

        /// <summary>
        /// ToString method.
        /// </summary>
        /// <param name="sb">String builder.</param>
        protected override void ToString(StringBuilder sb)
        {
            sb.Append("IFRM ");
            sb.Append(String.Format("{0} octets", m_payload.Length));
        }
    }
}
