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
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using OpenAX25Contracts;
using OpenAX25Core;
using System.Text;
using OpenAX25_Protocol;

namespace OpenAX25Router
{
	/// <summary>
	/// This class is a pseudo channel that routes forwarded packages to the
	/// corresponding interfaces. Routing is based on <c>mheard</c> rules and
	/// static XX.254 routes.
	/// </summary>
	public class RouterChannel : L2Channel
    {
		
		private IList<Route> m_routes = new List<Route>();
    	
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
		/// <description>Routes file [Default: Not set]</description>
		/// </item>
		/// </list>
		/// </param>
    	public RouterChannel(IDictionary<string,string> properties)
    		: base(FixRecursion(properties))
    	{
    		string _routes;
    		if (properties.TryGetValue("Routes", out _routes)) {
                string fileName = Path.IsPathRooted(_routes)?_routes:
                    Path.Combine(Path.GetDirectoryName(m_runtime.ConfigFileName), _routes);
                string schemaFile = Path.Combine(Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location), "OpenAX25Routes.xsd");
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, String.Format(
                        "Loading config schema file \"{0}\"", schemaFile));
                StreamReader xsdReader = new StreamReader(schemaFile);
                XmlSchema schema = XmlSchema.Read(xsdReader,
                    new ValidationEventHandler(XSDValidationEventHandler));

                // Config the XML reader settings:
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, String.Format(
                        "Loading XML route file \"{0}\"", fileName));
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.ValidationType = ValidationType.Schema;
                readerSettings.Schemas.Add(schema);
                readerSettings.ValidationEventHandler +=
                    new ValidationEventHandler(XMLValidationEventHandler);

                // Read in the XML file:
                XmlDocument doc = new XmlDocument();
                using (XmlTextReader textReader = new XmlTextReader(fileName))
                using (XmlReader reader = XmlReader.Create(textReader, readerSettings))
                    doc.Load(reader);
                XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);
                nsm.AddNamespace("route", "urn:OpenAX25Routes");

                // Get name:
                string name = ((XmlElement)doc.SelectSingleNode("/route:OpenAX25Routes", nsm))
                    .GetAttribute("name");

                m_runtime.Log(LogLevel.INFO, m_name,
                    String.Format("Using routes \"{0}\"", name));

                // Load routes:
                foreach (XmlElement e in doc.SelectNodes("//route:Route", nsm))
                {
                    string target = e.GetAttribute("target");
                    XmlElement patternElement = (XmlElement)e.SelectSingleNode("route:Pattern", nsm);
                    string pattern = (patternElement != null)?patternElement.InnerText.Trim():".*";
                    bool _continue = Boolean.Parse(e.GetAttribute("continue"));
                    IDictionary<string, string> routeProperties = new Dictionary<string, string>();
                    foreach (XmlElement p in e.SelectNodes("route:Property", nsm))
                        routeProperties.Add(p.GetAttribute("name"), p.InnerText.Trim());
                    m_runtime.Log(LogLevel.INFO, m_name, String.Format(
                        "Route \"{0}\" to \"{1}\" and {2} ({3})", pattern, target,
                            (_continue?"continue":"stop"), DumpProperties(routeProperties)));
                    m_routes.Add(new Route(target, pattern, _continue, routeProperties));
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
            IL2Channel targetChannel = null;
            IDictionary<string,string> targetProperties = null;
            foreach (Route r in m_routes)
                if (r.IsMatch(targetCall))
                {
                    targetChannel = r.Channel;
                    targetProperties = r.Properties;
                    break;
                }
            if (targetChannel != null)
            {
                string text = String.Format("{0} [{1}-->{2}]: {3}", header.ToString(),
                    header.nextHop.ToString(), targetChannel.Name,
                    //L2HexConverter.ToHexString(data, true)
                    AX25Frame.Create(data, header.isCommand, header.isResponse).ToString()
                );
                if (m_runtime.LogLevel >= LogLevel.DEBUG)
                    m_runtime.Log(LogLevel.DEBUG, m_name, text);
                m_runtime.Monitor(text);
                L2Frame new_frame = new L2Frame(m_runtime.NewFrameNo(), frame.isPriorityFrame,
                                frame.data, targetProperties);
                targetChannel.ForwardFrame(new_frame);
            }
            else
            {
                string text = String.Format("{0} [{1}--><NoRoute>]: {2}", header.ToString(),
                    header.nextHop.ToString(), HexConverter.ToHexString(data, true));
                m_runtime.Log(LogLevel.INFO, m_name, text);
                m_runtime.Monitor(text);
            }
    	}

        private void XSDValidationEventHandler(object sender, ValidationEventArgs e)
        {
            string msg = String.Format("XSD Validation Error: {0}", e.Message);
            switch (e.Severity)
            {
                case XmlSeverityType.Warning:
                    m_runtime.Log(LogLevel.WARNING, m_name, msg);
                    break;
                case XmlSeverityType.Error:
                    m_runtime.Log(LogLevel.ERROR, m_name, msg);
                    throw e.Exception;
            } // end switch //
        }

        private void XMLValidationEventHandler(object sender, ValidationEventArgs e)
        {
            string msg = String.Format("XSD Validation Error: {0}", e.Message);
            switch (e.Severity)
            {
                case XmlSeverityType.Warning:
                    m_runtime.Log(LogLevel.WARNING, m_name, msg);
                    break;
                case XmlSeverityType.Error:
                    m_runtime.Log(LogLevel.ERROR, m_name, msg);
                    throw e.Exception;
            } // end switch //
        }
    	
    	private static IDictionary<string,string> FixRecursion(IDictionary<string,string> properties)
    	{
            // Set default target to the null channel:
    		if (properties.ContainsKey("Target"))
    			properties.Remove("Target");
    		properties.Add("Target", "L2NULL");
    		return properties;
    	}

        private static string DumpProperties(IDictionary<string, string> properties)
        {
            if (properties == null)
                return "<null>";
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> entry in properties)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(entry.Key);
                sb.Append("=\"");
                sb.Append(entry.Value);
                sb.Append("\"");
            } // end foreach //
            return sb.ToString();
        }
    	
    }
}
