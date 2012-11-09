//
// IConnection.cs
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
    /// Receive a primitive.
    /// </summary>
    /// <param name="p"></param>
    public delegate void ReadEventHandler(IPrimitive p);

    /// <summary>
    /// IConnection.
    /// Connection from a local endpoint to a remote one.
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Gets the ID of the connection.
        /// </summary>
        /// <value>
        /// The unique ID of this connection.
        /// </value>
        string Id{ get; }

        /// <summary>
        /// Send a primitive over the connection.
        /// </summary>
        /// <param name="p">The primitive to send.</param>
        void Write(IPrimitive p);

        /// <summary>
        /// Receive a primitive from this connection.
        /// </summary>
        event ReadEventHandler Read;

        /// <summary>
        /// Source address.
        /// </summary>
        ILocalEndpoint[] Source { get; }

        /// <summary>
        /// Destination path. The Entries will be processed in the
        /// given order, so the last entry is the final destination.
        /// </summary>
        IRemoteEndpoint[] Destination { get; }

        /// <summary>
        /// Gets the properties of this connection.
        /// </summary>
        /// <value>
        /// The properties.
        /// </value>
        IDictionary<string, string> Properties { get; }
    }
}
