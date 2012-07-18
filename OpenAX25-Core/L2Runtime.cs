//
// L2Runtime.cs
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

namespace OpenAX25Core
{
	/// <summary>
	/// Core runtime object for OpenAX25.
	/// </summary>
	public class L2Runtime
	{
		
		private IL2LogProvider m_logProvider = null;
		private IL2MonitorProvider m_monitorProvider = null;
		private L2LogLevel m_logLevel = L2LogLevel.INFO;
		private IDictionary<string, IL2ChannelFactory> m_factories =
			new Dictionary<string, IL2ChannelFactory>();
		private IDictionary<string, IL2Channel> m_channels =
			new Dictionary<string, IL2Channel>();
		private UInt64 m_frameNo = 0;
		
		/// <summary>
		/// The one and only runtime object.
		/// </summary>
		public static readonly L2Runtime Instance = new L2Runtime();
		
		/// <summary>
		/// Creation of Runtime object is not possible.
		/// </summary>
		private L2Runtime()
		{
			// TODO: Load assemblies dynamically:
			// For now, we use static linking.
			
		}
		
		/// <summary>
		/// The log provider. If set to <c>null</c>, no logging is performed at all.
		/// </summary>
		public IL2LogProvider LogProvider {
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
		public L2LogLevel LogLevel {
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
		public IL2MonitorProvider MonitorProvider {
			get {
				return m_monitorProvider;
			}
			set {
				m_monitorProvider = value;
			}
		}
		
		/// <summary>
		/// Log a message if log level is equal or above the choosen loglevel and
		/// there was a log provider.
		/// </summary>
		/// <param name="level">The log level of this message.</param>
		/// <param name="component">The originating component.</param>
		/// <param name="message">The message</param>
		public void Log(L2LogLevel level, string component, string message)
		{
			if ((m_logProvider == null) || (level > m_logLevel))
				return;
			if (String.IsNullOrEmpty(component))
				component = "<Unknown>";
			if (String.IsNullOrEmpty(message))
				message = "<null>";
			lock(this) {
				try { m_logProvider.OnLog(component, message); } catch {}
			}
		}
		
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
		public IL2Channel CreateChannel(string channelClassName,
		                                IDictionary<string,string> properties)
		{
			if (String.IsNullOrEmpty(channelClassName))
				throw new ArgumentNullException("channelClassName");
			
			// If the short name was specified, lookup the full name:
			if (channelClassName.Split(new Char[] {':'}).Length < 2) {
				// Find the long name.
				channelClassName += ':';
				foreach (string key in m_factories.Keys) {
					if (key.StartsWith(channelClassName)) {
						channelClassName = key;
						break;
					}
				} // end foreach //
			}
			IL2ChannelFactory factory;
			if (!m_factories.TryGetValue(channelClassName, out factory))
				throw new L2FactoryNotFoundException(channelClassName);
			Log(L2LogLevel.INFO, "L2Runtime", "Creating channel of class: " + factory.ChannelClass);
			return factory.CreateChannel(properties);
		}
		
		/// <summary>
		/// Register a channel factory on the Runtime.
		/// </summary>
		/// <param name="factory">ChannelFactory to register.</param>
		public void RegisterFactory(IL2ChannelFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			m_factories.Add(factory.ChannelClass, factory);
			Log(L2LogLevel.INFO, "L2Runtime", "Registered channel factory: " + factory.ChannelClass);
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
		public void RegisterChannel(IL2Channel channel, string name=null)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");
			if (String.IsNullOrEmpty(name))
				name = channel.Name;
			// Check if the channel is registered already:
			IL2Channel _channel;
			if (m_channels.TryGetValue(name, out _channel)) {
				if (_channel.GetHashCode() == channel.GetHashCode()) {
					Log(L2LogLevel.INFO, "L2Runtime", "Reregistered channel " + name);
					return; // Registered already
				} else {
					Log(L2LogLevel.INFO, "L2Runtime", "Channel name is already taken: " + name);
					throw new L2DuplicateNameException("Name is choosen for another channel already: " + name);
				}
			} else {
				m_channels.Add(name, channel);
				Log(L2LogLevel.INFO, "L2Runtime", "Registered channel " + name);
			}
		}
		
		/// <summary>
		/// Lookup a channel with a name.
		/// </summary>
		/// <param name="name">The name to lookup.</param>
		/// <returns>Channel registered with this name of <c>null</c> if not found.</returns>
		public IL2Channel LookupChannel(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			IL2Channel result;
			if (m_channels.TryGetValue(name, out result))
				return result;
			else
				return null;
		}
		
		/// <summary>
		/// Get a new frame no.
		/// </summary>
		/// <returns>New frame number.</returns>
		public virtual UInt64 NewFrameNo()
		{
			lock (this) {
				unchecked {
					return this.m_frameNo++;
				}
			}
		}

	}
}
