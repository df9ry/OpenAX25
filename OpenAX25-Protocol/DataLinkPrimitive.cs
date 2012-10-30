using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenAX25_Protocol
{

    internal enum DataLinkPrimitive_T
    {
        DL_CONNECT_Request_T,
        DL_CONNECT_Indication_T,
        DL_CONNECT_Confirm_T,
        DL_DISCONNECT_Request_T,
        DL_DISCONNECT_Indication_T,
        DL_DISCONNECT_Confirm_T,
        DL_DATA_Request_T,
        DL_DATA_Indication_T,
        DL_UNIT_DATA_Request_T,
        DL_UNIT_DATA_Indication_T,
        DL_ERROR_Indication_T,
        DL_FLOW_OFF_Request_T,
        DL_FLOW_ON_Request_T
    }

    internal abstract class DataLinkPrimitive
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

        protected readonly DataLinkStateMachine m_dlsm; 

        protected DataLinkPrimitive(DataLinkStateMachine dlsm)
        {
            this.m_dlsm = dlsm;
        }

        internal abstract DataLinkPrimitive_T DataLinkPrimitiveType { get; }

        internal string DataLinkPrimitiveTypeName
        {
            get
            {
                return N[DataLinkPrimitiveType];
            }
        }
    }

    internal sealed class DL_CONNECT_Request : DataLinkPrimitive
    {
        private readonly AX25Modulo m_modulo;

        internal DL_CONNECT_Request(DataLinkStateMachine dlsm, AX25Modulo modulo)
            : base(dlsm)
        {
            m_modulo = modulo;
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType {
            get {
                return DataLinkPrimitive_T.DL_CONNECT_Request_T;
            }
        }

        internal AX25Modulo Modulo
        {
            get
            {
                return m_modulo;
            }
        }
    }

    internal sealed class DL_CONNECT_Indication : DataLinkPrimitive
    {
        private readonly AX25Modulo m_modulo;

        internal DL_CONNECT_Indication(DataLinkStateMachine dlsm, AX25Modulo modulo)
            : base(dlsm)
        {
            m_modulo = modulo;
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_CONNECT_Indication_T;
            }
        }

        internal AX25Modulo Modulo
        {
            get
            {
                return m_modulo;
            }
        }
    }

    internal sealed class DL_CONNECT_Confirm : DataLinkPrimitive
    {
        internal DL_CONNECT_Confirm(DataLinkStateMachine dlsm)
            : base(dlsm)
        {
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_CONNECT_Confirm_T;
            }
        }
    }

    internal sealed class DL_DISCONNECT_Request : DataLinkPrimitive
    {
        internal DL_DISCONNECT_Request(DataLinkStateMachine dlsm)
            : base(dlsm)
        {
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_DISCONNECT_Request_T;
            }
        }
    }

    internal sealed class DL_DISCONNECT_Indication : DataLinkPrimitive
    {
        internal DL_DISCONNECT_Indication(DataLinkStateMachine dlsm)
            : base(dlsm)
        {
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_DISCONNECT_Indication_T;
            }
        }
    }

    internal sealed class DL_DISCONNECT_Confirm : DataLinkPrimitive
    {
        internal DL_DISCONNECT_Confirm(DataLinkStateMachine dlsm)
            : base(dlsm)
        {
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_DISCONNECT_Confirm_T;
            }
        }
    }

    internal sealed class DL_DATA_Request : DataLinkPrimitive
    {
        private readonly byte[] m_data;

        internal DL_DATA_Request(DataLinkStateMachine dlsm, byte[] data)
            : base(dlsm)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            m_data = data;
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_DATA_Request_T;
            }
        }

        internal byte[] Data
        {
            get
            {
                return m_data;
            }
        }
    }

    internal sealed class DL_DATA_Indication : DataLinkPrimitive
    {
        private readonly byte[] m_data;

        internal DL_DATA_Indication(DataLinkStateMachine dlsm, byte[] data)
            : base(dlsm)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            m_data = data;
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_DATA_Indication_T;
            }
        }

        internal byte[] Data
        {
            get
            {
                return m_data;
            }
        }
    }

    internal sealed class DL_UNIT_DATA_Request : DataLinkPrimitive
    {
        private readonly byte[] m_data;

        internal DL_UNIT_DATA_Request(DataLinkStateMachine dlsm, byte[] data)
            : base(dlsm)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            m_data = data;
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_UNIT_DATA_Request_T;
            }
        }

        internal byte[] Data
        {
            get
            {
                return m_data;
            }
        }
    }

    internal sealed class DL_UNIT_DATA_Indication : DataLinkPrimitive
    {
        private readonly byte[] m_data;

        internal DL_UNIT_DATA_Indication(DataLinkStateMachine dlsm, byte[] data)
            : base(dlsm)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            m_data = data;
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_UNIT_DATA_Indication_T;
            }
        }

        internal byte[] Data
        {
            get
            {
                return m_data;
            }
        }
    }

    internal sealed class DL_ERROR_Indication : DataLinkPrimitive
    {
        private readonly  DataLinkStateMachine.ErrorCode_T m_erc;

        internal DL_ERROR_Indication(DataLinkStateMachine dlsm, DataLinkStateMachine.ErrorCode_T erc)
            : base(dlsm)
        {
            m_erc = erc;
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_ERROR_Indication_T;
            }
        }

        internal DataLinkStateMachine.ErrorCode_T ErrorCode
        {
            get
            {
                return m_erc;
            }
        }
    }

    internal sealed class DL_FLOW_OFF_Request : DataLinkPrimitive
    {
        internal DL_FLOW_OFF_Request(DataLinkStateMachine dlsm)
            : base(dlsm)
        {
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_FLOW_OFF_Request_T;
            }
        }
    }

    internal sealed class DL_FLOW_ON_Request : DataLinkPrimitive
    {
        internal DL_FLOW_ON_Request(DataLinkStateMachine dlsm)
            : base(dlsm)
        {
        }

        internal override DataLinkPrimitive_T DataLinkPrimitiveType
        {
            get
            {
                return DataLinkPrimitive_T.DL_FLOW_ON_Request_T;
            }
        }
    }

}
