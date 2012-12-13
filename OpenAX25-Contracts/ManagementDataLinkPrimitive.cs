//
// ManagementDataLinkPrimitive.cs
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

namespace OpenAX25Contracts
{
    /// <summary>
    /// Message to signal information over a L3 channel.
    /// </summary>
    public enum ManagementDataLinkPrimitive_T
    {
        /** <summary>Negotiate Request      </summary> */ MDL_NEGOTIATE_Request_T,
        /** <summary>Negotiate Confirmation </summary> */ MDL_NEGOTIATE_Confirm_T,
        /** <summary>Error Indication       </summary> */ MDL_ERROR_Indication_T
    }

    /// <summary>
    /// Abstract super class for all Management Data Link Primitives.
    /// </summary>
    public abstract class ManagementDataLinkPrimitive : IPrimitive
    {
        private static readonly IDictionary<ManagementDataLinkPrimitive_T, string> N =
            new Dictionary<ManagementDataLinkPrimitive_T, string> {
            { ManagementDataLinkPrimitive_T.MDL_NEGOTIATE_Request_T, "MDL_NEGOTIATE_Request" },
            { ManagementDataLinkPrimitive_T.MDL_NEGOTIATE_Confirm_T, "MDL_NEGOTIATE_Confirm" },
            { ManagementDataLinkPrimitive_T.MDL_ERROR_Indication_T,  "MDL_ERROR_Indication"  }
        };

        /// <summary>
        /// Standard constructor.
        /// </summary>
        protected ManagementDataLinkPrimitive()
            : base()
        {
        }

        /// <summary>
        /// Get Management Data Link Primitive type.
        /// </summary>
        public abstract ManagementDataLinkPrimitive_T ManagementDataLinkPrimitiveType { get; }

        /// <summary>
        /// Get human readable name of Management Data Link Primitive Type.
        /// </summary>
        public string ManagementDataLinkPrimitiveTypeName
        {
            get
            {
                return N[ManagementDataLinkPrimitiveType];
            }
        }
    }

    /// <summary>
    /// Negotiate Request.
    /// </summary>
    public sealed class MDL_NEGOTIATE_Request : ManagementDataLinkPrimitive
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public MDL_NEGOTIATE_Request()
            : base()
        {
        }

        /// <summary>
        /// Get Management Data Link Primitive type.
        /// </summary>
        public override ManagementDataLinkPrimitive_T ManagementDataLinkPrimitiveType
        {
            get
            {
                return ManagementDataLinkPrimitive_T.MDL_NEGOTIATE_Request_T;
            }
        }
    }

    /// <summary>
    /// Negotiate Confirmation.
    /// </summary>
    public sealed class MDL_NEGOTIATE_Confirm : ManagementDataLinkPrimitive
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public MDL_NEGOTIATE_Confirm()
            : base()
        {
        }

        /// <summary>
        /// Get Management Data Link Primitive type.
        /// </summary>
        public override ManagementDataLinkPrimitive_T ManagementDataLinkPrimitiveType
        {
            get
            {
                return ManagementDataLinkPrimitive_T.MDL_NEGOTIATE_Confirm_T;
            }
        }
    }

    /// <summary>
    /// Error Indication.
    /// </summary>
    public sealed class MDL_ERROR_Indication : ManagementDataLinkPrimitive
    {
        private readonly long m_erc;
        private readonly string m_description;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="erc">Error code number.</param>
        /// <param name="description">Error description.</param>
        public MDL_ERROR_Indication(long erc, string description)
            : base()
        {
            m_erc = erc;
            m_description = description;
        }

        /// <summary>
        /// Get Management Data Link Primitive type.
        /// </summary>
        public override ManagementDataLinkPrimitive_T ManagementDataLinkPrimitiveType
        {
            get
            {
                return ManagementDataLinkPrimitive_T.MDL_ERROR_Indication_T;
            }
        }

        /// <summary>
        /// Get error code number.
        /// </summary>
        public long ErrorCode
        {
            get
            {
                return m_erc;
            }
        }

        /// <summary>
        /// Get error description.
        /// </summary>
        public string Description
        {
            get
            {
                return m_description;
            }
        }
    }

}
