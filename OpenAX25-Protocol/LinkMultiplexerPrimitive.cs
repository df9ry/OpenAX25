using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{

    internal enum LinkMultiplexerPrimitive_T
    {
        LM_SEIZE_Request_T,
        LM_SEIZE_Confirm_T,
        LM_RELEASE_Request_T,
        LM_EXPEDITED_DATA_Request_T,
        LM_DATA_Request_T,
        LM_DATA_Indication_T
    }

    internal abstract class LinkMultiplexerPrimitive
    {

        private static readonly IDictionary<LinkMultiplexerPrimitive_T, string> N = new Dictionary<LinkMultiplexerPrimitive_T, string> {
            { LinkMultiplexerPrimitive_T.LM_DATA_Request_T,           "LM_DATA_Request"           },
            { LinkMultiplexerPrimitive_T.LM_DATA_Indication_T,        "LM_DATA_Indication"        },
            { LinkMultiplexerPrimitive_T.LM_SEIZE_Request_T,          "LM_SEIZE_Request"          },
            { LinkMultiplexerPrimitive_T.LM_SEIZE_Confirm_T,          "LM_SEIZE_Confirm"          },
            { LinkMultiplexerPrimitive_T.LM_EXPEDITED_DATA_Request_T, "LM_EXPEDITED_DATA_Request" },
            { LinkMultiplexerPrimitive_T.LM_RELEASE_Request_T,        "LM_RELEASE_Request"        }
        };

        protected readonly LinkMultiplexerStateMachine m_lmsm;

        protected LinkMultiplexerPrimitive(LinkMultiplexerStateMachine lmsm)
        {
            this.m_lmsm = lmsm;
        }

        internal abstract LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType { get; }

        internal string LinkMultiplexerPrimitiveTypeName
        {
            get
            {
                return N[LinkMultiplexerPrimitiveType];
            }
        }
    }

    /**
     * The Data-Link State Machine uses this primitive to request the Link Multiplexer
     * State machine to arrange for transmission at the next available opportunity. The Data-Link State
     * Machine uses this primitive when an acknowledgement must be made; the exact frame in which the
     * acknowledgement ist sent will be chosen when the actual time for transmission arrives.
     */
    internal sealed class LM_SEIZE_Request : LinkMultiplexerPrimitive
    {
        internal LM_SEIZE_Request(LinkMultiplexerStateMachine mlsm)
            : base(mlsm)
        {
        }

        internal override LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType
        {
            get
            {
                return LinkMultiplexerPrimitive_T.LM_SEIZE_Request_T;
            }
        }
    }

    /**
     * This primitive indicates to the Dat-Link State Machine that the transmission
     * opportunity has arrived.
     */
    internal sealed class LM_SEIZE_Confirm : LinkMultiplexerPrimitive
    {
        internal LM_SEIZE_Confirm(LinkMultiplexerStateMachine mlsm)
            : base(mlsm)
        {
        }

        internal override LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType
        {
            get
            {
                return LinkMultiplexerPrimitive_T.LM_SEIZE_Confirm_T;
            }
        }
    }

    /**
     * The Link Multiplexer State Machine uses this primitive to stop transmission.
     */
    internal sealed class LM_RELEASE_Request : LinkMultiplexerPrimitive
    {
        internal LM_RELEASE_Request(LinkMultiplexerStateMachine mlsm)
            : base(mlsm)
        {
        }

        internal override LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType
        {
            get
            {
                return LinkMultiplexerPrimitive_T.LM_RELEASE_Request_T;
            }
        }
    }

    /**
     * The Data-Link State Machine uses this primitive to pass expedited data
     * to the link multiplexer.
     */
    internal sealed class LM_EXPEDITED_DATA_Request : LinkMultiplexerPrimitive
    {
        private readonly byte[] m_data;

        internal LM_EXPEDITED_DATA_Request(LinkMultiplexerStateMachine mlsm, byte[] data)
            : base(mlsm)
        {
            m_data = data;
        }

        internal byte[] Data
        {
            get
            {
                return m_data;
            }
        }

        internal override LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType
        {
            get
            {
                return LinkMultiplexerPrimitive_T.LM_EXPEDITED_DATA_Request_T;
            }
        }
    }

    /**
     * The Data-Link State Machine uses this primitive to pass frames of any type
     * (SABM, RR, UI, etc.) to the Link Multiplexer State Machine.
     */
    internal sealed class LM_DATA_Request : LinkMultiplexerPrimitive
    {
        private readonly byte[] m_data;

        internal LM_DATA_Request(LinkMultiplexerStateMachine mlsm, byte[] data)
            : base(mlsm)
        {
            m_data = data;
        }

        internal byte[] Data
        {
            get
            {
                return m_data;
            }
        }

        internal override LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType
        {
            get
            {
                return LinkMultiplexerPrimitive_T.LM_DATA_Request_T;
            }
        }
    }

    /**
     * The Link Multiplexer State Machine uses this primitive to pass frames of any
     * type (SABM, RR, UI, etc.) to the Data-Link State Machine.
     */
    internal sealed class LM_DATA_Indication : LinkMultiplexerPrimitive
    {
        private readonly byte[] m_data;

        internal LM_DATA_Indication(LinkMultiplexerStateMachine mlsm, byte[] data)
            : base(mlsm)
        {
            m_data = data;
        }

        internal byte[] Data
        {
            get
            {
                return m_data;
            }
        }

        internal override LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType
        {
            get
            {
                return LinkMultiplexerPrimitive_T.LM_DATA_Indication_T;
            }
        }
    }

}
