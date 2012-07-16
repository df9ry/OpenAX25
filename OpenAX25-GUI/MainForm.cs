//
// OpenAX25-GUI.csproj
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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25GUI
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form, IL2LogProvider
	{
		
		private const int MAX_LOG_LINES = 1000;
		private const int MAX_MONITOR_LINES = 1000;
		private ArrayList m_logLines = new ArrayList(MAX_LOG_LINES);
		private ArrayList m_monitorLines = new ArrayList(MAX_MONITOR_LINES);
		
		private IL2Channel m_routerChannel;
		private IL2Channel m_kissChannel;
		
		public MainForm()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();

			L2Runtime.Instance.LogProvider = this;
			L2Runtime.Instance.LogLevel = L2LogLevel.DEBUG;
			
			L2Runtime.Instance.Log(L2LogLevel.INFO, "MainForm", "Program started");
			
			// Register the ROUTER Interface:
			L2Runtime.Instance.RegisterFactory(new OpenAX25Router.ChannelFactory());
			
			// Register the KISS Interface:
			L2Runtime.Instance.RegisterFactory(new OpenAX25Kiss.ChannelFactory());
		
			// Create ROUTER channel:
			m_routerChannel = L2Runtime.Instance.CreateChannel
				("ROUTER", new Dictionary<string,string>()
				{
				 	{ "Name",    "Router" },
            });

			// Create KISS channel:
			m_kissChannel = L2Runtime.Instance.CreateChannel
				("KISS", new Dictionary<string,string>()
				{
				 	{ "Name",    "Kiss" },
				 	{ "ComPort", "COM12"  },
	            });
			
			// Route KISS to ROUTER:
			m_kissChannel.Target = m_routerChannel;
			
			// Open Router channel:
			m_routerChannel.Open();
			
			// Open KISS channel:
			m_kissChannel.Open();
		}
		
		/// <summary>
		/// The logging implementation.
		/// </summary>
		/// <param name="component">Name of the component</param>
		/// <param name="message">Log message</param>
		public void OnLog(string component, string message)
		{
			string text = String.Format("{0}: {1}", component, message);
			AppendLog(text);
		}
		
		private void AppendLog(string text)
		{
			if (InvokeRequired)
			{
				this.Invoke(new Action<string>(AppendLog), new object[] {text});
				return;
			}
			
			string[] lines;
			if (m_logLines.Count >= MAX_LOG_LINES) {
				lines = (string[]) m_logLines.ToArray(typeof(string));
				for (int i = 1; i < lines.Length; ++i)
					lines[i-1] = lines[i];
				lines[m_logLines.Count - 1] = text;
			} else {
				m_logLines.Add(text);
				lines = (string[]) m_logLines.ToArray(typeof(string));
			}
			
			logTextBox.Lines = lines;
			logTextBox.SelectionLength = logTextBox.Text.Length;
			logTextBox.ScrollToCaret();
			logTextBox.SelectionLength = 0;
			logTextBox.Refresh();
		}

		private void AppendMonitor(string text)
		{
			if (InvokeRequired)
			{
				this.Invoke(new Action<string>(AppendMonitor), new object[] {text});
			}
			
			string[] lines;
			if (m_monitorLines.Count >= MAX_MONITOR_LINES) {
				lines = (string[]) m_monitorLines.ToArray(typeof(string));
				for (int i = 1; i < lines.Length; ++i)
					lines[i-1] = lines[i];
				lines[m_monitorLines.Count - 1] = text;
			} else {
				m_monitorLines.Add(text);
				lines = (string[]) m_monitorLines.ToArray(typeof(string));
			}
			
			monitorTextBox.Lines = lines;
			monitorTextBox.SelectionLength = monitorTextBox.Text.Length;
			monitorTextBox.ScrollToCaret();
			monitorTextBox.SelectionLength = 0;
			monitorTextBox.Refresh();
		}
		
		void MainFormFormClosing(object sender, FormClosingEventArgs e)
		{
			if (m_kissChannel != null)
				m_kissChannel.Close();
			if (m_routerChannel != null)
				m_routerChannel.Close();
		}
	}
}
