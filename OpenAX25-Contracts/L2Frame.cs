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
using System.Collections.Generic;
using System.Text;

namespace OpenAX25Contracts
{

	/// <summary>
	/// Holder object for transmit frame.
	/// </summary>
	public struct L2Frame
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="no">Frame number.</param>
		/// <param name="isPriorityFrame">When <c>true</c> the frame is a
		/// priortity frame.</param>
		/// <param name="data">Frame data</param>
		public L2Frame (UInt64 no, bool isPriorityFrame, byte[] data)
		{
			this.no = no;
			this.isPriorityFrame = isPriorityFrame;
			this.data = data;
			if (data == null)
				throw new ArgumentNullException("data");
			this.addr = new Dictionary<string,string>();
		}
		
		/// <summary>
		/// Address information for this frame.
		/// </summary>
		public readonly IDictionary<string,string> addr;

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
		
		/// <summary>
		/// Test is this frame is the empty one.
		/// </summary>
		/// <returns></returns>
		public bool IsEmpty()
		{
			return (Empty.GetHashCode() == this.GetHashCode());
		}
		
		/// <summary>
		/// Test is the frame is the empty one.
		/// </summary>
		/// <param name="frame">If this is the empty one.</param>
		/// <returns></returns>
		public static bool IsEmpty(L2Frame frame)
		{
			return frame.IsEmpty();
		}
		
		/// <summary>
		/// The <c>Empty</c> L2Frame, used for saying <c>Nothing</c>.
		/// </summary>
		public static readonly L2Frame Empty = new L2Frame(0, false, new byte[0]);
	}
}

