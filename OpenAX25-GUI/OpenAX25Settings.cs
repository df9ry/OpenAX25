//
// OpenAX25Settings.cs
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
using System.Configuration;
using System.IO;

using OpenAX25Contracts;

namespace OpenAX25GUI
{
	/// <summary>
	/// User settings for this application.
	/// </summary>
	sealed class OpenAX25Settings : ApplicationSettingsBase
	{
		
		private bool dirty = false;
		
		[UserScopedSetting()]
		[DefaultSettingValue("INFO")]
		public L2LogLevel LogLevel {
			get {
				return (L2LogLevel)this["LogLevel"];
			}
			set {
				if ((L2LogLevel)this["LogLevel"] == value)
					return;
				this["LogLevel"] = value;
				dirty = true;
			}
		}
		
		[UserScopedSetting()]
		[DefaultSettingValue("True")]
		public Boolean MonitorOn {
			get {
				return (Boolean)this["MonitorOn"];
			}
			set {
				if ((Boolean)this["MonitorOn"] == value)
					return;
				this["MonitorOn"] = value;
				dirty = true;
			}
		}
		
		[UserScopedSetting()]
		[DefaultSettingValue("")]
		public String ConfigFile {
			get {
				if (String.IsNullOrEmpty((String)this["ConfigFile"])) {
					this["ConfigFile"] = "OpenAX25Config.xml";
					dirty = true;
				}
				return (String)this["ConfigFile"];
			}
			set {
				if (((String)this["ConfigFile"]).Equals(value))
					return;
				this["ConfigFile"] = value;
				dirty = true;
			}
		}
		
		public override void Save()
		{
			if (dirty)
				base.Save();
		}
		
	}
}
