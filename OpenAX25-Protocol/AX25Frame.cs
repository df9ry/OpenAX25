using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        protected AX25Frame(byte[] octets, AX25Modulo modulo)
        {
            m_octets = octets;
            m_modulo = modulo;
        }

        public abstract AX25Frame_T FrameType { get; }

        public string FrameTypeName
        {
            get
            {
                return N[FrameType];
            }
        }

        public virtual bool Poll
        {
            get
            {
                return false;
            }
        }

        public virtual bool Final
        {
            get
            {
                return false;
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

        public static AX25Frame Create(byte[] octets, bool isCommand, bool isResponse,
            DataLinkStateMachine.Version_T version = DataLinkStateMachine.Version_T.V2_0)
        {
            AX25Modulo modulo = (version == DataLinkStateMachine.Version_T.V2_2) ?
                AX25Modulo.MOD128 : AX25Modulo.MOD8;
            if (octets == null)
                throw new ArgumentNullException("octets");
            if (octets.Length == 0)
                return new AX25InvalidFrame(octets);
            if ((octets[0] & 0x01) == 0x00) // I-Frame:
                return new AX25_I(octets, modulo);
            if ((octets[0] & 0x02) == 0x00) // S-Frame:
                return AX25SFrame.Create(octets, modulo);
            else
                return AX25UFrame.Create(octets);
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
