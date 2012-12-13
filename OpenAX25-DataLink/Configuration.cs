//
// Configuration.cs
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

using OpenAX25Contracts;

namespace OpenAX25_DataLink
{
    internal struct Configuration
    {
        internal Configuration(string name)
        {
            this.name = name;
            this.Initial_SAT = 300;
            this.Initial_SRT = 3000;
            this.Initial_N1 = 32767;
            this.Initial_N2 = 10;
            this.Initial_version = AX25Version.V2_0;
            this.Header = new AX25Header(L2Callsign.CQ, L2Callsign.CQ,
                new L2Callsign[0]);
        }

        internal string name;

        internal long Initial_SAT;
        internal long Initial_SRT;
        internal long Initial_N1;
        internal long Initial_N2;
        internal AX25Version Initial_version;
        internal AX25Header Header;
    }
}
