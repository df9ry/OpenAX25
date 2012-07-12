//
// TxFrame.cs
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

namespace OpenAX25Core
{

	/// <summary>
	/// Holder object for transmit frame.
	/// </summary>
	public struct L2TxFrame
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="no">Frame number.</param>
		/// <param name="isPriorityFrame">When <c>true</c> the frame is a
		/// priortity frame.</param>
		/// <param name="data">Frame data</param>
		public L2TxFrame (UInt64 no, bool isPriorityFrame, byte[] data)
		{
			this.no = no;
			this.isPriorityFrame = isPriorityFrame;
			this.data = data;
		}

		/// <summary>
		/// Frame number.
		/// </summary>
		public readonly UInt64 no;
		
		/// <summary>
		/// When <c>true</c> the frame is a priority frame.
		/// </summary>
		public readonly bool isPriorityFrame;
		
		/// <summary>
		/// Frame data.
		/// </summary>
		public readonly byte[] data;
	}
}

