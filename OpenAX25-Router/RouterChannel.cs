//
// KissInterface.cs
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
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25Router
{
	/// <summary>
	/// This class is a pseudo channel that routes forwarded packages to the
	/// corresponding interfaces. Routing is based on <c>mheard</c> rules and
	/// static XX.254 routes.
	/// </summary>
	public class RouterChannel : L2Channel
    {
		
		private IDictionary<string, IDictionary<string,string>> m_routes =
			new Dictionary<string, IDictionary<string,string>>();
    	
		/// <summary>
		/// Construct new RouterChannel.
		/// </summary>
		/// <param name="properties">Properties of the channel.
		/// <list type="bullet">
		/// <listheader>
		/// <term>Property name</term>
		/// <description>Description</description>
		/// </listheader>
		/// <item>
		/// <term>Name</term>
		/// <description>Name of the interface [mandatory]</description>
		/// </item>
		/// <item>
		/// <term>Routes</term>
		/// <description>Static routes [Default: empty]</description>
		/// </item>
		/// </list>
		/// </param>
    	public RouterChannel(IDictionary<string,string> properties)
    		: base(FixRecursion(properties))
    	{
    		string _routes;
    		if (properties.TryGetValue("Routes", out _routes)) {
    			foreach (string _route in _routes.Split('&')) {
    				int p = _route.IndexOf(':');
    				if (p < 0)
    					throw new L2InvalidPropertyException("Syntax error: ':' missing: " + _route);
    				string destination = _route.Substring(0, p);
    				string _properties = _route.Substring(p+1);
    				IDictionary<string,string> property_map = new Dictionary<string,string>();
    				foreach (string _property in _properties.Split(',')) {
    					int q = _property.IndexOf('=');
    					if (q < 0)
	    					throw new L2InvalidPropertyException("Syntax error: '=' missing: " + _property);
    					string name = _property.Substring(0, q);
    					string value = _property.Substring(q+1);
    					property_map.Add(name, value);
    				} // end foreach //
    				m_routes.Add(destination, property_map);
    			} // end foreach //
    		}
    	}
    	
    	/// <summary>
    	/// Here the frames are coming in.
    	/// </summary>
    	/// <param name="frame">The frame to route.</param>
    	protected override void OnForward(L2Frame frame)
    	{
    		L2Header header = new L2Header(frame.data);
    		// Get frame data:
    		int iData = 0;
    		for (; iData < frame.data.Length; ++iData)
    			if ((frame.data[iData] & 0x01) != 0x00)
    				break;
    		byte[] data = new Byte[frame.data.Length - iData - 1];
    		Array.Copy(frame.data, iData+1, data, 0, data.Length);
    		// Perform routing:
    		string targetCall = header.nextHop.ToString();
	    	IDictionary<string,string> targetProperties;
	    	string targetChannel;
	    	if (!m_routes.TryGetValue(targetCall, out targetProperties)) {
				targetProperties = null;
				targetChannel = "NULL";
	    	} else {
	    		if (!targetProperties.TryGetValue("Channel", out targetChannel)) {
	    			targetChannel = "NULL";
	    		}
	    	}
    		string text = String.Format("{0} [{1}->{2}]: {3}", header.ToString(), targetChannel,
	    	                            header.nextHop.ToString(), L2HexConverter.ToHexString(data, true));
    		m_runtime.Log(L2LogLevel.INFO, m_name, text);
    		m_runtime.Monitor(text);
    		IL2Channel ch = m_runtime.LookupChannel(targetChannel);
    		if (ch != null) {
    			L2Frame new_frame = new L2Frame(m_runtime.NewFrameNo(), frame.isPriorityFrame,
    			                                frame.data, targetProperties);
    			ch.ForwardFrame(new_frame);
    		} else {
    			m_runtime.Log(L2LogLevel.WARNING, m_name, "Channel not found: " + targetChannel);
    		}
    	}
    	
    	private static IDictionary<string,string> FixRecursion(IDictionary<string,string> properties)
    	{
            // Set default target to the null channel:
    		if (properties.ContainsKey("Target"))
    			properties.Remove("Target");
    		properties.Add("Target", "NULL");
    		return properties;
    	}
    	
    }
}
