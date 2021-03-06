﻿//
// Runtime.cs
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

using OpenAX25Contracts;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Xml.Schema;
using System.Xml;
using System.Text;

namespace OpenAX25Core
{
	/// <summary>
	/// Core runtime object for OpenAX25.
	/// </summary>
	public sealed class Runtime
	{
		
		private ILogProvider m_logProvider = null;
		private IMonitorProvider m_monitorProvider = null;
		private LogLevel m_logLevel = LogLevel.INFO;
		private IDictionary<string, IChannelFactory> m_factories =
			new Dictionary<string, IChannelFactory>();
		private IDictionary<string, IChannel> m_channels =
			new Dictionary<string, IChannel>();
		private UInt64 m_frameNo = 0;
        private XmlSchema m_configSchema = null;
        private string m_configFileName = "";
        private string m_configName = "";

        private static Runtime instance = null;
        private static Object instanceLock = new Object();
		
		/// <summary>
		/// The one and only runtime object.
		/// </summary>
        public static Runtime Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (instanceLock)
                    {
                        if (instance == null)
                            instance = new Runtime();
                    }
                }
                return instance;
            }
            set
            {
                instance = value;
            }
        }
		
		/// <summary>
		/// Creation of Runtime object is not possible.
		/// </summary>
		public Runtime()
		{
            RegisterChannel(L2NullChannel.Instance);
            RegisterChannel(L3NullChannel.Instance);
        }
		
		/// <summary>
		/// The log provider. If set to <c>null</c>, no logging is performed at all.
		/// </summary>
		public ILogProvider LogProvider {
			get {
				return m_logProvider;
			}
			set {
				m_logProvider = value;
			}
		}
		
		/// <summary>
		/// The log level to control the amount of data to display.
		/// </summary>
		public LogLevel LogLevel {
			get {
				return m_logLevel;
			}
			set {
				m_logLevel = value;
			}
		}
		
		/// <summary>
		/// The monitor provider. If set to <c>null</c>, no monitoring is performed at all.
		/// </summary>
		public IMonitorProvider MonitorProvider {
			get {
				return m_monitorProvider;
			}
			set {
				m_monitorProvider = value;
			}
		}

        /// <summary>
        /// The full name of the current config file.
        /// </summary>
        public string ConfigFileName
        {
            get
            {
                return m_configFileName;
            }
        }

        /// <summary>
        /// The name of the current config.
        /// </summary>
        public string ConfigName
        {
            get
            {
                return m_configName;
            }
        }
		
		/// <summary>
		/// Log a message if log level is equal or above the choosen loglevel and
		/// there was a log provider.
		/// </summary>
		/// <param name="level">The log level of this message.</param>
		/// <param name="component">The originating component.</param>
		/// <param name="message">The message</param>
		public void Log(LogLevel level, string component, string message)
		{
			if ((m_logProvider == null) || (level > m_logLevel))
				return;
			if (String.IsNullOrEmpty(component))
				component = "<Unknown>";
			if (String.IsNullOrEmpty(message))
				message = "<null>";
			lock(this) {
				try { m_logProvider.OnLog(component, level + ": " + message); } catch {}
			}
		}
		
		/// <summary>
		/// Add a line of text to the monitor window.
		/// </summary>
		/// <param name="text">Text to show in the monitor window.</param>
		public void Monitor(string text)
		{
			if ((m_monitorProvider == null) || String.IsNullOrEmpty(text))
				return;
			lock(this) {
				try { m_monitorProvider.OnMonitor(text); } catch {}
			}
		}
		
		/// <summary>
		/// Create a new channel with the given channel class name and properties. The new channel
		/// is automatically registered with the runtime.
		/// </summary>
		/// <param name="channelClassName">Channel class name (With or without the GUID</param>
		/// <param name="properties">Channel properties for the new instance.</param>
		/// <returns>New channel instance.</returns>
		public IChannel CreateChannel(string channelClassName,
		                                IDictionary<string,string> properties)
		{
			if (String.IsNullOrEmpty(channelClassName))
				throw new ArgumentNullException("channelClassName");
			
			// If the short name was specified, lookup the full name:
			if (channelClassName.Split(new Char[] {':'}).Length < 2) {
                Log(LogLevel.DEBUG, "Runtime", "Lookup long name of class: " + channelClassName);
                // Find the long name.
				string _channelClassName = channelClassName + ':';
				foreach (string key in m_factories.Keys) {
					if (key.StartsWith(_channelClassName)) {
						channelClassName = key;
						break;
					}
				} // end foreach //
			}
			IChannelFactory factory;
			if (!m_factories.TryGetValue(channelClassName, out factory))
				throw new FactoryNotFoundException(channelClassName);
			Log(LogLevel.DEBUG, "Runtime", "Creating channel of class: " + factory.ChannelClass);
			return factory.CreateChannel(properties);
		}
		
		/// <summary>
		/// Register a channel factory on the Runtime.
		/// </summary>
		/// <param name="factory">ChannelFactory to register.</param>
		public void RegisterFactory(IChannelFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			m_factories.Add(factory.ChannelClass, factory);
			Log(LogLevel.INFO, "Runtime", "Registered factory: " + factory.ChannelClass);
		}
		
		/// <summary>
		/// Registers a channel with the runtime, so that it can be lookuped
		/// in the future. If the channel is registered in the runtime already
		/// nothing happens.
		/// <remarks>It is perfectly OK to register a channel with more than
		/// just one name.</remarks>
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="name">Name to register the channel with. If not specified or <c>null</c>
		/// the value of the <c>Name</c> property of the channel is used.</param>
		public void RegisterChannel(IChannel channel, string name=null)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");
			if (String.IsNullOrEmpty(name))
				name = channel.Name;
			// Check if the channel is registered already:
			IChannel _channel;
			if (m_channels.TryGetValue(name, out _channel)) {
				if (_channel.GetHashCode() == channel.GetHashCode()) {
					Log(LogLevel.INFO, "Runtime", "Reregistered channel " + name);
					return; // Registered already
				} else {
					Log(LogLevel.INFO, "Runtime", "Channel name is already taken: " + name);
					throw new DuplicateNameException("Name is choosen for another channel already: " + name);
				}
			} else {
				m_channels.Add(name, channel);
				Log(LogLevel.INFO, "Runtime", "Registered channel " + name);
			}
		}
		
		/// <summary>
		/// Lookup a channel with a name.
		/// </summary>
		/// <param name="name">The name to lookup.</param>
		/// <returns>Channel registered with this name of <c>null</c> if not found.</returns>
		public IChannel LookupChannel(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			IChannel result;
			if (m_channels.TryGetValue(name, out result))
				return result;
			else
				return null;
		}

        /// <summary>
        /// Lookup a L2 channel with a name.
        /// </summary>
        /// <param name="name">The name to lookup.</param>
        /// <returns>Channel registered with this name of <c>null</c> if not found.</returns>
        public IL2Channel LookupL2Channel(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            IChannel result;
            if (m_channels.TryGetValue(name, out result))
            {
                if (result is IL2Channel)
                    return (IL2Channel)result;
                else
                    return null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Lookup a L3 channel with a name.
        /// </summary>
        /// <param name="name">The name to lookup.</param>
        /// <returns>Channel registered with this name of <c>null</c> if not found.</returns>
        public IL3Channel LookupL3Channel(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            IChannel result;
            if (m_channels.TryGetValue(name, out result))
            {
                if (result is IL3Channel)
                    return (IL3Channel)result;
                else
                    return null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Load a channel assembly into the runtime.
        /// </summary>
        /// <param name="fileName">Path of the assembly. If this path is relative,
        /// it is looked up in the directory where the main assembly is loaded from.</param>
        public void LoadAssembly(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (!Path.IsPathRooted(fileName))
                fileName = Path.Combine(Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location), fileName);

            // Load assembly:
            if (this.LogLevel >= LogLevel.DEBUG)
                this.Log(LogLevel.DEBUG, "Runtime", String.Format("Loading assembly \"{0}\"", fileName));
            Assembly a = Assembly.LoadFile(fileName);
            if (a == null)
                throw new Exception(String.Format("Unable to load assembly \"{0}\"",
                    fileName));

            // Lookup factories:
            foreach (Type t in a.GetTypes())
            {
                if (t.GetInterfaces().Contains(typeof(IChannelFactory)))
                {
                    if (this.LogLevel >= LogLevel.DEBUG)
                        this.Log(LogLevel.DEBUG, "Runtime", String.Format("Create factory \"{0}\"", t.FullName));
                    IChannelFactory f = Activator.CreateInstance(t) as IChannelFactory;
                    if (f == null)
                        throw new Exception(
                            String.Format("Unable to create instance of type \"{0}\"", t.FullName));
                    this.RegisterFactory(f);
                }
            } // end foreach //
        }

        /// <summary>
        /// Load XML configuration file.
        /// </summary>
        /// <param name="fileName">Path of the configuration file. If this path is relative,
        /// it is looked up in the directory where the main assembly is loaded from.</param>
        public void LoadConfig(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (Path.IsPathRooted(fileName))
                m_configFileName = fileName;
            else
                m_configFileName = Path.Combine(Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location), fileName);

            lock (this)
            {
                // Assure the config schema:
                if (m_configSchema == null)
                {
                    string configSchemaFile = Path.Combine(Path.GetDirectoryName(
                        Assembly.GetExecutingAssembly().Location), "OpenAX25Config.xsd");
                    if (this.LogLevel >= LogLevel.DEBUG)
                        this.Log(LogLevel.DEBUG, "Runtime",
                            String.Format("Loading config schema file \"{0}\"",
                            configSchemaFile));
                    StreamReader xsdReader = new StreamReader(configSchemaFile);
                    m_configSchema = XmlSchema.Read(xsdReader,
                        new ValidationEventHandler(XSDValidationEventHandler));
                }

                // Config the XML reader settings:
                if (this.LogLevel >= LogLevel.DEBUG)
                    this.Log(LogLevel.DEBUG, "Runtime",
                        String.Format("Loading XML config file \"{0}\"",
                        fileName));
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.ValidationType = ValidationType.Schema;
                readerSettings.Schemas.Add(m_configSchema);
                readerSettings.ValidationEventHandler +=
                    new ValidationEventHandler(XMLValidationEventHandler);

                // Read in the XML file:
                XmlDocument doc = new XmlDocument();
                using (XmlTextReader textReader = new XmlTextReader(m_configFileName))
                    using (XmlReader reader = XmlReader.Create(textReader, readerSettings))
                        doc.Load(reader);
                XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);
                nsm.AddNamespace("cnf", "urn:OpenAX25Config");

                // Get config name:
                m_configName = ((XmlElement)doc.SelectSingleNode("/cnf:OpenAX25Config", nsm))
                    .GetAttribute("name");

                this.Log(LogLevel.INFO, "Runtime",
                    String.Format("Using configuration \"{0}\"", m_configName));
                
                // Load assemblies:
                foreach (XmlElement e in doc.SelectNodes("//cnf:Assembly", nsm))
                    this.LoadAssembly(e.GetAttribute("file"));

                // Create channels:
                IList<IChannel> channelsToOpen = new List<IChannel>();
                foreach (XmlElement e in doc.SelectNodes("//cnf:Channel", nsm))
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>();
                    properties.Add("Name", e.GetAttribute("name"));
                    properties.Add("Target", e.GetAttribute("target"));

                    // Set custom options:
                    foreach (XmlElement p in e.SelectNodes("cnf:Property", nsm))
                        properties.Add(p.GetAttribute("name"), p.InnerText);

                    // Create the channel:
                    IChannel channel = this.CreateChannel(e.GetAttribute("class"), properties);
                    if (channel == null)
                        throw new Exception(String.Format(
                            "Factory for class {0} returned NULL object",
                            e.GetAttribute("class")));

                    // Open the channel if requested:
                    if (Boolean.Parse(e.GetAttribute("open")))
                        channelsToOpen.Add(channel);
                } // end foreach //

                // Open channels:
                foreach (IChannel channel in channelsToOpen)
                    channel.Open();
            } // end lock //
        }

		/// <summary>
		/// Get a new frame no.
		/// </summary>
		/// <returns>New frame number.</returns>
		public UInt64 NewFrameNo()
		{
			lock (this) {
				unchecked {
					return this.m_frameNo++;
				}
			}
		}

        /// <summary>
        /// Shutdown the runtime object. Cannot be used afterwards.
        /// </summary>
        public void Shutdown()
        {
            Log(LogLevel.INFO, "Runtime", "Shutdown");
            foreach (IChannel ch in m_channels.Values)
            {
                try
                {
                    ch.Close();
                }
                catch (Exception e)
                {
                    Log(LogLevel.WARNING, ch.Name, "Shutdown failure: " + e.Message);
                }
            } // end foreach //
            m_channels.Clear();
            Runtime.Instance = null;
        }

        /// <summary>
        /// Prints detailled stack trace information to the log.
        /// </summary>
        /// <param name="ll">Log level</param>
        /// <param name="module">Module reporting the failure</param>
        /// <param name="message">User message</param>
        /// <param name="e">Exception</param>
        public void StackTrace(LogLevel ll, string module, string message, Exception e)
        {
            if (LogLevel < ll)
                return;
            StringBuilder sb = new StringBuilder();
            sb.Append(message);
            while (e != null)
            {
                sb.Append("\n*** ");
                sb.Append(e.GetType().Name);
                sb.Append(" ***");
                sb.Append("\n Message: ");
                sb.Append(e.Message);
                sb.Append("\n Source : ");
                sb.Append(e.Source);
                sb.Append("\n Target : ");
                sb.Append(e.TargetSite);
                sb.Append("\n Stack  :\n");
                sb.Append(e.StackTrace);
                e = e.InnerException;
                if (e != null)
                    sb.Append("\n Inner:\n");
            } // end while //
            Log(ll, module, sb.ToString());
        }

        private void XSDValidationEventHandler(object sender, ValidationEventArgs e)
        {
            string msg = String.Format("XSD Validation Error: {0}", e.Message);
            switch (e.Severity)
            {
                case XmlSeverityType.Warning:
                    this.Log(LogLevel.WARNING, "Runtime", msg);
                    break;
                case XmlSeverityType.Error:
                    this.Log(LogLevel.ERROR, "Runtime", msg);
                    throw e.Exception;
            } // end switch //
        }

        private void XMLValidationEventHandler(object sender, ValidationEventArgs e)
        {
            string msg = String.Format("XSD Validation Error: {0}", e.Message);
            switch (e.Severity)
            {
                case XmlSeverityType.Warning:
                    this.Log(LogLevel.WARNING, "Runtime", msg);
                    break;
                case XmlSeverityType.Error:
                    this.Log(LogLevel.ERROR, "Runtime", msg);
                    throw e.Exception;
            } // end switch //
        }

    }
}
