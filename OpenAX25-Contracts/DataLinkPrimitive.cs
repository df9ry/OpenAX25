//
// DataLinkPrimitive.cs
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
    public enum DataLinkPrimitive_T
    {
        /** <summary>Connect Request        </summary> */ DL_CONNECT_Request_T,
        /** <summary>Connect Indication     </summary> */ DL_CONNECT_Indication_T,
        /** <summary>Connect Confirmation   </summary> */ DL_CONNECT_Confirm_T,
        /** <summary>Disconnect Request     </summary> */ DL_DISCONNECT_Request_T,
        /** <summary>Disconnect Indication  </summary> */ DL_DISCONNECT_Indication_T,
        /** <summary>Disconnect Confirmation</summary> */ DL_DISCONNECT_Confirm_T,
        /** <summary>Data Request           </summary> */ DL_DATA_Request_T,
        /** <summary>Data Indication        </summary> */ DL_DATA_Indication_T,
        /** <summary>UI Data Request        </summary> */ DL_UNIT_DATA_Request_T,
        /** <summary>UI Data Indication     </summary> */ DL_UNIT_DATA_Indication_T,
        /** <summary>Error Indication       </summary> */ DL_ERROR_Indication_T,
        /** <summary>Flow Off Request       </summary> */ DL_FLOW_OFF_Request_T,
        /** <summary>Flow On Request        </summary> */ DL_FLOW_ON_Request_T
    }

    /// <summary>
    /// Abstract super class for all Data Link Primitives.
    /// </summary>
    public abstract class DataLinkPrimitive : IPrimitive
    {
        private static readonly IDictionary<DataLinkPrimitive_T, string> N = new Dictionary<DataLinkPrimitive_T, string> {
            { DataLinkPrimitive_T.DL_CONNECT_Request_T, "DL_CONNECT_Request" },
            { DataLinkPrimitive_T.DL_CONNECT_Indication_T, "DL_CONNECT_Indication" },
            { DataLinkPrimitive_T.DL_CONNECT_Confirm_T, "DL_CONNECT_Confirm" },
            { DataLinkPrimitive_T.DL_DISCONNECT_Request_T, "DL_DISCONNECT_Request" },
            { DataLinkPrimitive_T.DL_DISCONNECT_Indication_T, "DL_DISCONNECT_Indication" },
            { DataLinkPrimitive_T.DL_DISCONNECT_Confirm_T, "DL_DISCONNECT_Confirm" },
            { DataLinkPrimitive_T.DL_DATA_Request_T, "DL_DATA_Request" },
            { DataLinkPrimitive_T.DL_DATA_Indication_T, "DL_DATA_Indication" },
            { DataLinkPrimitive_T.DL_UNIT_DATA_Request_T, "DL_UNIT_DATA_Request" },
            { DataLinkPrimitive_T.DL_UNIT_DATA_Indication_T, "DL_UNIT_DATA_Indication" },
            { DataLinkPrimitive_T.DL_ERROR_Indication_T, "DL_ERROR_Indication" },
            { DataLinkPrimitive_T.DL_FLOW_OFF_Request_T, "DL_FLOW_OFF_Request" },
            { DataLinkPrimitive_T.DL_FLOW_ON_Request_T, "DL_FLOW_ON_Request" },
        };

        /// <summary>
        /// Standard constructor.
        /// </summary>
        protected DataLinkPrimitive()
        {
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public abstract DataLinkPrimitive_T DataLinkPrimitiveType { get; }

        /// <summary>
        /// Get human readable name of Data Link Primitive Type.
        /// </summary>
        public string DataLinkPrimitiveTypeName
        {
            get
            {
                return N[DataLinkPrimitiveType];
            }
        }
    }

    /// <summary>
    /// Connect Request.
    /// </summary>
    public sealed class DL_CONNECT_Request : DataLinkPrimitive
    {
        private readonly AX25Modulo m_modulo;
        private readonly string m_remoteAddr;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="remoteAddr">Address of the remote station.</param>
        /// <param name="modulo">AX.25 Modulo format.</param>
        public DL_CONNECT_Request(string remoteAddr, AX25Modulo modulo = AX25Modulo.MOD8)
            : base()
        {
            if (remoteAddr == null)
                throw new ArgumentNullException("remoteAddr");
            m_remoteAddr = remoteAddr;
            if (modulo == AX25Modulo.UNSPECIFIED)
                modulo = AX25Modulo.MOD8;
            m_modulo = modulo;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="remoteAddr">Address of the remote station.</param>
        /// <param name="version">AX.25 Version.</param>
        public DL_CONNECT_Request(string remoteAddr, AX25Version version)
            : base()
        {
            if (remoteAddr == null)
                throw new ArgumentNullException("remoteAddr");
            m_remoteAddr = remoteAddr;
            m_modulo = (version == AX25Version.V2_0) ? AX25Modulo.MOD8 : AX25Modulo.MOD128;
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get {
                return DataLinkPrimitive_T.DL_CONNECT_Request_T;
            }
        }

        /// <summary>
        /// Get remote address.
        /// </summary>
        public string RemoteAddr
        {
            get
            {
                return m_remoteAddr;
            }
        }

        /// <summary>
        /// Get AX.25 Modulo format.
        /// </summary>
        public AX25Modulo Modulo
        {
            get
            {
                return m_modulo;
            }
        }

        /// <summary>
        /// Get AX.25 Version.
        /// </summary>
        public AX25Version Version
        {
            get {
                return (m_modulo == AX25Modulo.MOD128)?
                    AX25Version.V2_2:
                    AX25Version.V2_0;
            }
        }
    }

    /// <summary>
    /// Connect Indication.
    /// </summary>
    public sealed class DL_CONNECT_Indication : DataLinkPrimitive
    {
        private readonly AX25Modulo m_modulo;
        private readonly string m_remoteAddr;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="remoteAddr">Address of the remote station.</param>
        /// <param name="modulo">AX.25 Modulo format.</param>
        public DL_CONNECT_Indication(string remoteAddr, AX25Modulo modulo)
            : base()
        {
            if (remoteAddr == null)
                throw new ArgumentNullException("remoteAddr");
            m_remoteAddr = remoteAddr;
            m_modulo = modulo;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="remoteAddr">Address of the remote station.</param>
        /// <param name="version">AX.25 Version.</param>
        public DL_CONNECT_Indication(string remoteAddr, AX25Version version)
            : base()
        {
            if (remoteAddr == null)
                throw new ArgumentNullException("remoteAddr");
            m_remoteAddr = remoteAddr;
            m_modulo = (version == AX25Version.V2_0) ? AX25Modulo.MOD8 : AX25Modulo.MOD128;
        }

        /// <summary>
        /// Get remote address.
        /// </summary>
        public string RemoteAddr
        {
            get
            {
                return m_remoteAddr;
            }
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_CONNECT_Indication_T;
            }
        }

        /// <summary>
        /// Get AX.25 Modulo format.
        /// </summary>
        public AX25Modulo Modulo
        {
            get
            {
                return m_modulo;
            }
        }

        /// <summary>
        /// Get AX.25 Version.
        /// </summary>
        public AX25Version Version
        {
            get
            {
                return (m_modulo == AX25Modulo.MOD128) ?
                    AX25Version.V2_2 :
                    AX25Version.V2_0;
            }
        }
    }

    /// <summary>
    /// Connect Confirmation.
    /// </summary>
    public sealed class DL_CONNECT_Confirm : DataLinkPrimitive
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DL_CONNECT_Confirm()
            : base()
        {
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_CONNECT_Confirm_T;
            }
        }
    }

    /// <summary>
    /// Disconnect Request.
    /// </summary>
    public sealed class DL_DISCONNECT_Request : DataLinkPrimitive
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DL_DISCONNECT_Request()
            : base()
        {
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_DISCONNECT_Request_T;
            }
        }
    }

    /// <summary>
    /// Disconnect Indication.
    /// </summary>
    public sealed class DL_DISCONNECT_Indication : DataLinkPrimitive
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DL_DISCONNECT_Indication()
            : base()
        {
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_DISCONNECT_Indication_T;
            }
        }
    }

    /// <summary>
    /// Disconnect Confirmation.
    /// </summary>
    public sealed class DL_DISCONNECT_Confirm : DataLinkPrimitive
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DL_DISCONNECT_Confirm()
            : base()
        {
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_DISCONNECT_Confirm_T;
            }
        }
    }

    /// <summary>
    /// Data Request.
    /// </summary>
    public sealed class DL_DATA_Request : DataLinkPrimitive
    {
        private readonly byte[] m_data;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">Payload data</param>
        public DL_DATA_Request(byte[] data)
            : base()
        {
            if (data == null)
                throw new ArgumentNullException("data");
            m_data = data;
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_DATA_Request_T;
            }
        }

        /// <summary>
        /// Get payload data.
        /// </summary>
        public byte[] Data
        {
            get
            {
                return m_data;
            }
        }
    }

    /// <summary>
    /// Data indication.
    /// </summary>
    public sealed class DL_DATA_Indication : DataLinkPrimitive
    {
        private readonly byte[] m_data;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">Payload data.</param>
        public DL_DATA_Indication(byte[] data)
            : base()
        {
            if (data == null)
                throw new ArgumentNullException("data");
            m_data = data;
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_DATA_Indication_T;
            }
        }

        /// <summary>
        /// Get payload data.
        /// </summary>
        public byte[] Data
        {
            get
            {
                return m_data;
            }
        }
    }

    /// <summary>
    /// UI Data Request.
    /// </summary>
    public sealed class DL_UNIT_DATA_Request : DataLinkPrimitive
    {
        private readonly byte[] m_data;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">Payload data.</param>
        public DL_UNIT_DATA_Request(byte[] data)
            : base()
        {
            if (data == null)
                throw new ArgumentNullException("data");
            m_data = data;
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_UNIT_DATA_Request_T;
            }
        }

        /// <summary>
        /// Get payload data.
        /// </summary>
        public byte[] Data
        {
            get
            {
                return m_data;
            }
        }
    }

    /// <summary>
    /// UI Data Indication.
    /// </summary>
    public sealed class DL_UNIT_DATA_Indication : DataLinkPrimitive
    {
        private readonly byte[] m_data;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">Payload data.</param>
        public DL_UNIT_DATA_Indication(byte[] data)
            : base()
        {
            if (data == null)
                throw new ArgumentNullException("data");
            m_data = data;
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_UNIT_DATA_Indication_T;
            }
        }

        /// <summary>
        /// Get payload data.
        /// </summary>
        public byte[] Data
        {
            get
            {
                return m_data;
            }
        }
    }

    /// <summary>
    /// Error Indication.
    /// </summary>
    public sealed class DL_ERROR_Indication : DataLinkPrimitive
    {
        private readonly long m_erc;
        private readonly string m_description;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="erc">Error code number.</param>
        /// <param name="description">Error description.</param>
        public DL_ERROR_Indication(long erc, string description)
            : base()
        {
            m_erc = erc;
            m_description = description;
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_ERROR_Indication_T;
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

    /// <summary>
    /// Flow Off Request.
    /// </summary>
    public sealed class DL_FLOW_OFF_Request : DataLinkPrimitive
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DL_FLOW_OFF_Request()
            : base()
        {
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_FLOW_OFF_Request_T;
            }
        }
    }

    /// <summary>
    /// Flow On Request.
    /// </summary>
    public sealed class DL_FLOW_ON_Request : DataLinkPrimitive
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public DL_FLOW_ON_Request()
            : base()
        {
        }

        /// <summary>
        /// Get Data Link Primitive type.
        /// </summary>
        public override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_FLOW_ON_Request_T;
            }
        }
    }

}
