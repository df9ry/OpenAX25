//
// Route.cs
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
using System.Text.RegularExpressions;
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25Router
{
    internal struct Route
    {
        private readonly string m_channelName;
        private readonly Regex m_regularExpression;
        private readonly bool m_continue;
        private readonly IDictionary<string, string> m_properties;
        private IL2Channel m_channel;

        internal Route(string target, string pattern, bool _continue,
            IDictionary<string,string>properties)
        {
            if (target == null)
                throw new NullReferenceException("target");
            if (pattern == null)
                throw new NullReferenceException("pattern");
            m_channelName = target;
            m_regularExpression = new Regex(pattern);
            m_continue = _continue;
            m_properties = properties;
            m_channel = null;
        }

        internal bool Continue
        {
            get
            {
                return m_continue;
            }
        }

        internal bool IsMatch(string call)
        {
            if (call == null)
                throw new NullReferenceException("call");
            return m_regularExpression.IsMatch(call);
        }

        internal IL2Channel Channel
        {
            get
            {
                if (m_channel == null) // Resolve lazy:
                    m_channel = Runtime.Instance.LookupL2Channel(m_channelName);
                if (m_channel == null)
                    throw new NotFoundException(m_channelName);
                return m_channel;
            }
        }

        internal IDictionary<string, string> Properties
        {
            get
            {
                return m_properties;
            }
        }
    }
}
