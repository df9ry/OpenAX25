//
// L2LogLevel.cs
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
	/// Description of LogLevel.
	/// </summary>
	public enum L2LogLevel
	{
		/// <summary>
		/// No messages are logged at all.
		/// </summary>
		NONE,
		
		/// <summary>
		/// Errors are logged.
		/// </summary>
		ERROR,
		
		/// <summary>
		/// Errors and informational messages are logged
		/// (Default).
		/// </summary>
		INFO,
		
		/// <summary>
		/// All messages are logged, including full hex dumps
		/// of data frames.
		/// </summary>
		DEBUG
	}
}
