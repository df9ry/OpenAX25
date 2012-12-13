//
// AX25Payload.cs
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

using System;
using System.Collections.Generic;
using System.Text;

namespace OpenAX25Contracts
{

    /// <summary>
    /// AX.25 Frame Type.
    /// </summary>
    public enum AX25Frame_T
    {
        /** <summary> Invalid frame </summary> */ _INV,
        /** <summary> SABME         </summary> */ SABME,
        /** <summary> SABM          </summary> */ SABM,
        /** <summary> DISC          </summary> */ DISC,
        /** <summary> DM            </summary> */ DM,
        /** <summary> FRMR          </summary> */ FRMR,
        /** <summary> REJ           </summary> */ REJ,
        /** <summary> RNR           </summary> */ RNR,
        /** <summary> RR            </summary> */ RR,
        /** <summary> SREJ          </summary> */ SREJ,
        /** <summary> UA            </summary> */ UA,
        /** <summary> I             </summary> */ I,
        /** <summary> UI            </summary> */ UI,
        /** <summary> TEST          </summary> */ TEST,
        /** <summary> XID           </summary> */ XID
    }


    /// <summary>
    /// AX.25 Frame.
    /// </summary>
    public abstract class AX25Payload
    {
        /** <summary>Data bytes.  </summary> */ protected          byte[]     m_payload;
        /** <summary>AX.25 Modulo.</summary> */ protected readonly AX25Modulo m_modulo;
        /** <summary>Command?     </summary> */ protected readonly bool       m_command;
        /** <summary>Response?    </summary> */ protected readonly bool       m_response;

        private static IDictionary<AX25Frame_T, string> N = new Dictionary<AX25Frame_T, string>
        {
            { AX25Frame_T._INV,  "_INV"  },
            { AX25Frame_T.SABME, "SABME" },
            { AX25Frame_T.SABM,  "SABM"  },
            { AX25Frame_T.DISC,  "DISC"  },
            { AX25Frame_T.DM,    "DM"    },
            { AX25Frame_T.FRMR,  "FRMR"  },
            { AX25Frame_T.REJ,   "REJ"   },
            { AX25Frame_T.RNR,   "RNR"   },
            { AX25Frame_T.RR,    "RR"    },
            { AX25Frame_T.SREJ,  "SREJ"  },
            { AX25Frame_T.UA,    "UA"    },
            { AX25Frame_T.I,     "I"     },
            { AX25Frame_T.UI,    "UI"    },
            { AX25Frame_T.TEST,  "TEST"  },
            { AX25Frame_T.XID,   "XID"   }
        };

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="payload">Data bytes.</param>
        /// <param name="modulo">AX.25 Modulo.</param>
        /// <param name="cmd">Command?</param>
        /// <param name="rsp">Response?</param>
        protected AX25Payload(byte[] payload, AX25Modulo modulo, bool cmd, bool rsp)
        {
            m_payload = payload;
            m_modulo = modulo;
            m_command = cmd;
            m_response = rsp;
        }

        /// <summary>
        /// Frame Type.
        /// </summary>
        public abstract AX25Frame_T FrameType { get; }

        /// <summary>
        /// Frame Type Name.
        /// </summary>
        public string FrameTypeName
        {
            get
            {
                return N[FrameType];
            }
        }

        /// <summary>
        /// Command.
        /// </summary>
        public bool Command
        {
            get
            {
                return (m_command && !m_response);
            }
        }

        /// <summary>
        /// Response.
        /// </summary>
        public bool Response
        {
            get
            {
                return (m_response && !m_command);
            }
        }

        /// <summary>
        /// AX.25 V 1.x frame.
        /// </summary>
        public bool PreviousVersion
        {
            get
            {
                return (m_command == m_response);
            }
        }

        /// <summary>
        /// Create AX.25 element from frame data.
        /// </summary>
        /// <param name="payload">Data bytes.</param>
        /// <param name="cmd">Command?</param>
        /// <param name="rsp">Response?</param>
        /// <param name="version">AX.25 version to use.</param>
        /// <returns>AX.25 element.</returns>
        public static AX25Payload Create(byte[] payload, bool cmd, bool rsp,
            AX25Version version)
        {
            AX25Modulo modulo = (version == AX25Version.V2_2) ?
                AX25Modulo.MOD128 : AX25Modulo.MOD8;
            return AX25Payload.Create(payload, cmd, rsp, version);
        }

        /// <summary>
        /// Create AX.25 element from frame data.
        /// </summary>
        /// <param name="frame">Data bytes.</param>
        /// <param name="iFrame">Index where payload starts</param>
        /// <param name="cmd">Command?</param>
        /// <param name="rsp">Response?</param>
        /// <param name="version">AX.25 version to use.</param>
        /// <returns>AX.25 element.</returns>
        public static AX25Payload Create(byte[] frame, int iFrame, bool cmd, bool rsp,
            AX25Version version)
        {
            AX25Modulo modulo = (version == AX25Version.V2_2) ?
                AX25Modulo.MOD128 : AX25Modulo.MOD8;
            return AX25Payload.Create(frame, iFrame, cmd, rsp, version);
        }

        /// <summary>
        /// Create AX.25 element from frame data.
        /// </summary>
        /// <param name="payload">Data bytes.</param>
        /// <param name="cmd">Command?</param>
        /// <param name="rsp">Response?</param>
        /// <param name="modulo">AX.25 Modulo</param>
        /// <returns>AX.25 element.</returns>
        public static AX25Payload Create(byte[] payload, bool cmd, bool rsp,
            AX25Modulo modulo)
        {
            if (payload == null)
                throw new ArgumentNullException("frame");
            if (payload.Length == 0)
                return new AX25InvalidFrame(payload, cmd, rsp);
            if ((payload[0] & 0x01) == 0x00) // I-Frame:
                return new AX25_I(payload, modulo, cmd, rsp);
            if ((payload[0] & 0x02) == 0x00) // S-Frame:
                return AX25S.Create(payload, modulo, cmd, rsp);
            else
                return AX25U.Create(payload, cmd, rsp);
        }

        /// <summary>
        /// Create AX.25 element from frame data.
        /// </summary>
        /// <param name="frame">Data bytes.</param>
        /// <param name="iFrame">Index where payload starts</param>
        /// <param name="cmd">Command?</param>
        /// <param name="rsp">Response?</param>
        /// <param name="modulo">AX.25 Modulo</param>
        /// <returns>AX.25 element.</returns>
        public static AX25Payload Create(byte[] frame, int iFrame, bool cmd, bool rsp, AX25Modulo modulo)
        {
            if (frame == null)
                throw new ArgumentNullException("frame");
            int lPayload = frame.Length - iFrame;
            if (lPayload < 1)
                throw new Exception("Frame too short");
            byte[] payload = new byte[lPayload];
            Array.Copy(frame, iFrame, payload, 0, lPayload);
            return Create(payload, cmd, rsp, modulo);
        }

        /// <summary>
        /// Size of the header for a AX.25 modulo.
        /// </summary>
        /// <param name="modulo">AX.25 Modulo.</param>
        /// <returns>Size of the modulo.</returns>
        protected static int ModuloSize(AX25Modulo modulo)
        {
            switch (modulo)
            {
                case AX25Modulo.UNSPECIFIED: return 0;
                case AX25Modulo.MOD8: return 1;
                case AX25Modulo.MOD128: return 2;
                default: throw new ArgumentOutOfRangeException("modulo");
            } // end switch //
        }

        /// <summary>
        /// Get frame as byte array.
        /// </summary>
        public byte[] Octets
        {
            get
            {
                return m_payload;
            }
        }

        /// <summary>
        /// To String method.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }

        /// <summary>
        /// To String method.
        /// </summary>
        /// <param name="sb">StringBuilder.</param>
        protected virtual void ToString(StringBuilder sb)
        {
        }
    }

}
