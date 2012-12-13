//
// PhysicalLayerPrimitive.cs
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
using System.Linq;
using System.Text;

namespace OpenAX25Contracts
{
    /// <summary>
    /// Message to signal information over a L3 channel.
    /// </summary>
    public enum PhysicalLayerPrimitive_T
    {
        /** <summary>Seize Request          </summary> */ PH_SEIZE_Request_T,
        /** <summary>Seize Confirm          </summary> */ PH_SEIZE_Confirm_T,
        /** <summary>Data Request           </summary> */ PH_DATA_Request_T,
        /** <summary>Release Request        </summary> */ PH_RELEASE_Request_T,
        /** <summary>Expedited Data Request </summary> */ PH_EXPEDITED_DATA_Request_T,
        /** <summary>Data Indication        </summary> */ PH_DATA_Indication_T,
        /** <summary>Busy Indication        </summary> */ PH_BUSY_Indication_T,
        /** <summary>Quiet Indication       </summary> */ PH_QUIET_Indication_T
    }

    /// <summary>
    /// Abstract super class for all Physical Layer Primitives.
    /// </summary>
    public abstract class PhysicalLayerPrimitive : IPrimitive
    {
        private static readonly IDictionary<PhysicalLayerPrimitive_T, string> N =
            new Dictionary<PhysicalLayerPrimitive_T, string> {
            { PhysicalLayerPrimitive_T.PH_SEIZE_Request_T,          "PH_SEIZE_Request"          },
            { PhysicalLayerPrimitive_T.PH_SEIZE_Confirm_T,          "PH_SEIZE_Confirm"          },
            { PhysicalLayerPrimitive_T.PH_DATA_Request_T,           "PH_DATA_Request"           },
            { PhysicalLayerPrimitive_T.PH_RELEASE_Request_T,        "PH_RELEASE_Request"        },
            { PhysicalLayerPrimitive_T.PH_EXPEDITED_DATA_Request_T, "PH_EXPEDITED_DATA_Request" },
            { PhysicalLayerPrimitive_T.PH_DATA_Indication_T,        "PH_DATA_Indication"        },
            { PhysicalLayerPrimitive_T.PH_BUSY_Indication_T,        "PH_BUSY_Indication"        },
            { PhysicalLayerPrimitive_T.PH_QUIET_Indication_T,       "PH_QUIET_Indication"       }
        };

        /// <summary>
        /// Standard constructor.
        /// </summary>
        protected PhysicalLayerPrimitive()
        {
        }

        /// <summary>
        /// Get Physical Layer Primitive type.
        /// </summary>
        public abstract PhysicalLayerPrimitive_T PhysicalLayerPrimitiveType { get; }

        /// <summary>
        /// Get human readable name of Physical Layer Primitive Type.
        /// </summary>
        public string PhysicalLayerPrimitiveTypeName
        {
            get
            {
                return N[PhysicalLayerPrimitiveType];
            }
        }
    }

    /// <summary>
    /// Seize Request.
    /// </summary>
    public sealed class PH_SEIZE_Request : PhysicalLayerPrimitive
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PH_SEIZE_Request()
            : base()
        {
        }

        /// <summary>
        /// Get Physical Layer Primitive type.
        /// </summary>
        public override PhysicalLayerPrimitive_T PhysicalLayerPrimitiveType
        {
            get
            {
                return PhysicalLayerPrimitive_T.PH_SEIZE_Request_T;
            }
        }
    }

    /// <summary>
    /// Seize Confirm.
    /// </summary>
    public sealed class PH_SEIZE_Confirm : PhysicalLayerPrimitive
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PH_SEIZE_Confirm()
            : base()
        {
        }

        /// <summary>
        /// Get Physical Layer Primitive type.
        /// </summary>
        public override PhysicalLayerPrimitive_T PhysicalLayerPrimitiveType
        {
            get
            {
                return PhysicalLayerPrimitive_T.PH_SEIZE_Confirm_T;
            }
        }
    }

    /// <summary>
    /// Data Request.
    /// </summary>
    public sealed class PH_DATA_Request : PhysicalLayerPrimitive
    {
        private readonly AX25Frame  m_frame;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="frame">AX.25 frame.</param>
        public PH_DATA_Request(AX25Frame frame)
            : base()
        {
            m_frame = frame;
        }

        /// <summary>
        /// Get Physical Layer Primitive type.
        /// </summary>
        public override PhysicalLayerPrimitive_T PhysicalLayerPrimitiveType
        {
            get
            {
                return PhysicalLayerPrimitive_T.PH_DATA_Request_T;
            }
        }

        /// <summary>
        /// AX.25 header.
        /// </summary>
        public AX25Frame Frame
        {
            get
            {
                return m_frame;
            }
        }
    }

    /// <summary>
    /// Release Request.
    /// </summary>
    public sealed class PH_RELEASE_Request : PhysicalLayerPrimitive
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PH_RELEASE_Request()
            : base()
        {
        }

        /// <summary>
        /// Get Physical Layer Primitive type.
        /// </summary>
        public override PhysicalLayerPrimitive_T PhysicalLayerPrimitiveType
        {
            get
            {
                return PhysicalLayerPrimitive_T.PH_RELEASE_Request_T;
            }
        }
    }

    /// <summary>
    /// Expedited Data Request.
    /// </summary>
    public sealed class PH_EXPEDITED_DATA_Request : PhysicalLayerPrimitive
    {
        private readonly AX25Frame m_frame;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="frame">AX.25 frame.</param>
        public PH_EXPEDITED_DATA_Request(AX25Frame frame)
            : base()
        {
            m_frame = frame;
        }

        /// <summary>
        /// Get Physical Layer Primitive type.
        /// </summary>
        public override PhysicalLayerPrimitive_T PhysicalLayerPrimitiveType
        {
            get
            {
                return PhysicalLayerPrimitive_T.PH_EXPEDITED_DATA_Request_T;
            }
        }

        /// <summary>
        /// AX.25 frame.
        /// </summary>
        public AX25Frame Frame
        {
            get
            {
                return m_frame;
            }
        }
    }

    /// <summary>
    /// Data Indication.
    /// </summary>
    public sealed class PH_DATA_Indication : PhysicalLayerPrimitive
    {
        private readonly AX25Frame m_frame;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="frame">AX.25 frame.</param>
        public PH_DATA_Indication(AX25Frame frame)
            : base()
        {
            m_frame = frame;
        }

        /// <summary>
        /// Get Physical Layer Primitive type.
        /// </summary>
        public override PhysicalLayerPrimitive_T PhysicalLayerPrimitiveType
        {
            get
            {
                return PhysicalLayerPrimitive_T.PH_DATA_Indication_T;
            }
        }

        /// <summary>
        /// AX.25 frame.
        /// </summary>
        public AX25Frame Frame
        {
            get
            {
                return m_frame;
            }
        }
    }

    /// <summary>
    /// Busy Indication.
    /// </summary>
    public sealed class PH_BUSY_Indication : PhysicalLayerPrimitive
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PH_BUSY_Indication()
            : base()
        {
        }

        /// <summary>
        /// Get Physical Layer Primitive type.
        /// </summary>
        public override PhysicalLayerPrimitive_T PhysicalLayerPrimitiveType
        {
            get
            {
                return PhysicalLayerPrimitive_T.PH_BUSY_Indication_T;
            }
        }
    }

    /// <summary>
    /// Quiet Indication.
    /// </summary>
    public sealed class PH_QUIET_Indication : PhysicalLayerPrimitive
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PH_QUIET_Indication()
            : base()
        {
        }

        /// <summary>
        /// Get Physical Layer Primitive type.
        /// </summary>
        public override PhysicalLayerPrimitive_T PhysicalLayerPrimitiveType
        {
            get
            {
                return PhysicalLayerPrimitive_T.PH_QUIET_Indication_T;
            }
        }
    }

}
