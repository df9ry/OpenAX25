//
// ILocalEndpoint.cs
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
    /// Instance that represents a local endpoint for a communication.
    /// </summary>
    public interface ILocalEndpoint : IEndpoint
    {

        /// <summary>
        /// Attach a new session.
        /// </summary>
        /// <param name="receiver">Receiver channel.</param>
        /// <param name="properties">Properties of the channel.
        /// <list type="bullet">
        ///   <listheader><term>Property name</term><description>Description</description></listheader>
        ///   <item><term>Name</term><description>Name of the channel [Mandatory]</description></item>
        /// </list>
        /// </param>
        /// <param name="alias">Name alias for better tracing [Default: Value of "Name"]</param>
        /// <returns>Transmitter channel</returns>
        IL3Channel Bind(IL3Channel receiver, IDictionary<string,string> properties, string alias = null);

        /// <summary>
        /// Unattach a session that where previously registered.
        /// </summary>
        /// <param name="transmitter">
        /// Tranmitter returned from previous call to Bind.
        /// </param>
        void Unbind(IL3Channel transmitter);
    }
}
