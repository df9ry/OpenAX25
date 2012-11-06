﻿//
// L2DuplicateNameException.cs
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

namespace OpenAX25Contracts
{
	/// <summary>
	/// Description of L2DuplicateNameException.
	/// </summary>
	public class DuplicateNameException : Exception
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message">The message attached to this exception.</param>
		public DuplicateNameException(string message) : base("Duplicate name: " + message) {}
		
		/// <summary>
		/// Cionstructor.
		/// </summary>
		/// <param name="message">The message attached to this exception.</param>
		/// <param name="innerException">Inner exception attached to this
		/// exception</param>
		public DuplicateNameException(string message, Exception innerException)
            : base("Duplicate name: " + message, innerException) { }
	}
}