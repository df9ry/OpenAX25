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
        LM_RELEASE_Request_T
    }

    internal abstract class LinkMultiplexerPrimitive
    {

        private static readonly IDictionary<LinkMultiplexerPrimitive_T, string> N = new Dictionary<LinkMultiplexerPrimitive_T, string> {
            { LinkMultiplexerPrimitive_T.LM_SEIZE_Request_T,    "LM_SEIZE_Request"  },
            { LinkMultiplexerPrimitive_T.LM_SEIZE_Confirm_T,    "LM_SEIZE_Confirm"  },
            { LinkMultiplexerPrimitive_T.LM_RELEASE_Request_T, "LM_RELEASE_Request" }
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

}
