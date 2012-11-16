using System;
using System.Collections.Generic;
using System.Text;
using OpenAX25Contracts;

namespace OpenAX25_Protocol
{

    public enum AX25Frame_T
    {
        _INV, SABME, SABM, DISC, DM, FRMR, REJ, RNR, RR, SREJ, UA, I, UI, TEST, XID
    }

    public abstract class AX25Frame
    {

        protected          byte[]     m_octets;
        protected readonly AX25Modulo m_modulo;
        protected readonly bool       m_command;
        protected readonly bool       m_response;

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

        protected AX25Frame(byte[] octets, AX25Modulo modulo, bool cmd, bool rsp)
        {
            m_octets = octets;
            m_modulo = modulo;
            m_command = cmd;
            m_response = rsp;
        }

        public abstract AX25Frame_T FrameType { get; }

        public string FrameTypeName
        {
            get
            {
                return N[FrameType];
            }
        }

        public bool Command
        {
            get
            {
                return (m_command && !m_response);
            }
        }

        public bool Response
        {
            get
            {
                return (m_response && !m_command);
            }
        }

        public bool PreviousVersion
        {
            get
            {
                return (m_command == m_response);
            }
        }

        public static AX25Frame Create(byte[] octets, bool cmd, bool rsp,
            AX25Version version)
        {
            AX25Modulo modulo = (version == AX25Version.V2_2) ?
                AX25Modulo.MOD128 : AX25Modulo.MOD8;
            return AX25Frame.Create(octets, cmd, rsp, version);
        }

        public static AX25Frame Create(byte[] octets, bool cmd, bool rsp,
            AX25Modulo modulo)
        {
            if (octets == null)
                throw new ArgumentNullException("octets");
            if (octets.Length == 0)
                return new AX25InvalidFrame(octets, cmd, rsp);
            if ((octets[0] & 0x01) == 0x00) // I-Frame:
                return new AX25_I(octets, modulo, cmd, rsp);
            if ((octets[0] & 0x02) == 0x00) // S-Frame:
                return AX25SFrame.Create(octets, modulo, cmd, rsp);
            else
                return AX25UFrame.Create(octets, cmd, rsp);
        }

        protected static int Size(AX25Modulo modulo)
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
                return m_octets;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }

        protected virtual void ToString(StringBuilder sb)
        {
        }
    }

}
