//
// LinkMultiplexerPrimitive.cs
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

using System.Collections.Generic;

namespace OpenAX25Contracts
{

    /// <summary>
    /// Link Multiplexer Primitive Type.
    /// </summary>
    public enum LinkMultiplexerPrimitive_T
    {
        /** <summary>SEIZE Request   </summary>*/ LM_SEIZE_Request_T,
        /** <summary>SEIZE Confirm   </summary>*/ LM_SEIZE_Confirm_T,
        /** <summary>Release Request </summary>*/ LM_RELEASE_Request_T,
        /** <summary>Release Confirm </summary>*/ LM_EXPEDITED_DATA_Request_T,
        /** <summary>Data Request    </summary>*/ LM_DATA_Request_T,
        /** <summary>Data Indication </summary>*/ LM_DATA_Indication_T
    }

    /// <summary>
    /// Base class for all Link Multiplexer Primitives.
    /// </summary>
    public abstract class LinkMultiplexerPrimitive : IPrimitive
    {

        private static readonly IDictionary<LinkMultiplexerPrimitive_T, string> N = new Dictionary<LinkMultiplexerPrimitive_T, string> {
            { LinkMultiplexerPrimitive_T.LM_DATA_Request_T,           "LM_DATA_Request"           },
            { LinkMultiplexerPrimitive_T.LM_DATA_Indication_T,        "LM_DATA_Indication"        },
            { LinkMultiplexerPrimitive_T.LM_SEIZE_Request_T,          "LM_SEIZE_Request"          },
            { LinkMultiplexerPrimitive_T.LM_SEIZE_Confirm_T,          "LM_SEIZE_Confirm"          },
            { LinkMultiplexerPrimitive_T.LM_EXPEDITED_DATA_Request_T, "LM_EXPEDITED_DATA_Request" },
            { LinkMultiplexerPrimitive_T.LM_RELEASE_Request_T,        "LM_RELEASE_Request"        }
        };

        /// <summary>
        /// Constructor.
        /// </summary>
        protected LinkMultiplexerPrimitive()
        {
        }

        /// <summary>
        /// Link Multiplexer Type.
        /// </summary>
        public abstract LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType { get; }

        /// <summary>
        /// Link Multiplexer Type Name.
        /// </summary>
        public string LinkMultiplexerPrimitiveTypeName
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
    public sealed class LM_SEIZE_Request : LinkMultiplexerPrimitive
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public LM_SEIZE_Request()
            : base()
        {
        }

        /// <summary>
        /// Link Multiplexer Type.
        /// </summary>
        public override LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType
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
    public sealed class LM_SEIZE_Confirm : LinkMultiplexerPrimitive
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public LM_SEIZE_Confirm()
            : base()
        {
        }

        /// <summary>
        /// Link Multiplexer Type.
        /// </summary>
        public override LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType
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
    public sealed class LM_RELEASE_Request : LinkMultiplexerPrimitive
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public LM_RELEASE_Request()
            : base()
        {
        }

        /// <summary>
        /// Link Multiplexer Type.
        /// </summary>
        public override LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType
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
    public sealed class LM_EXPEDITED_DATA_Request : LinkMultiplexerPrimitive
    {
        private readonly AX25Frame m_frame;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="frame">AX.25 frame.</param>
        public LM_EXPEDITED_DATA_Request(AX25Frame frame)
            : base()
        {
            m_frame = frame;
        }

        /// <summary>
        /// AX.25 Frame.
        /// </summary>
        public AX25Frame Frame
        {
            get
            {
                return m_frame;
            }
        }

        /// <summary>
        /// Link Multiplexer Type.
        /// </summary>
        public override LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType
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
    public sealed class LM_DATA_Request : LinkMultiplexerPrimitive
    {
        private readonly AX25Frame m_frame;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="frame">AX.25 frame.</param>
        public LM_DATA_Request(AX25Frame frame)
            : base()
        {
            m_frame = frame;
        }

        /// <summary>
        /// AX.25 Frame.
        /// </summary>
        public AX25Frame Frame
        {
            get
            {
                return m_frame;
            }
        }

        /// <summary>
        /// Link Multiplexer Type.
        /// </summary>
        public override LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType
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
    public sealed class LM_DATA_Indication : LinkMultiplexerPrimitive
    {
        private readonly AX25Frame m_frame;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="frame">AX.25 frame.</param>
        public LM_DATA_Indication(AX25Frame frame)
            : base()
        {
            m_frame = frame;
        }

        /// <summary>
        /// AX.25 Frame.
        /// </summary>
        public AX25Frame Frame
        {
            get
            {
                return m_frame;
            }
        }

        /// <summary>
        /// Link Multiplexer Type.
        /// </summary>
        public override LinkMultiplexerPrimitive_T LinkMultiplexerPrimitiveType
        {
            get
            {
                return LinkMultiplexerPrimitive_T.LM_DATA_Indication_T;
            }
        }
    }

}
