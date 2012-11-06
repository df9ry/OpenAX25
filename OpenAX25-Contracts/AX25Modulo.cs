//
// AX25Modulo.cs
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

namespace OpenAX25Contracts
{
    /// <summary>
    /// AX.25 Modulo indicator.
    /// </summary>
    public enum AX25Modulo {
        /** <summary>Modulo not specified     </summary>*/ UNSPECIFIED = 0,
        /** <summary>Modulo is 3 bits [0..7]  </summary>*/ MOD8 = 8,
        /** <summary>Modulo is 7 bits [0..127]</summary>*/ MOD128 = 128
    }
}
