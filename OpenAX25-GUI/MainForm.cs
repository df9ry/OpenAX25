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
using System.IO;
using System.Windows.Forms;

using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25GUI
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form, ILogProvider, IMonitorProvider
	{
		
		private const int MAX_LOG_LINES = 1000;
		private const int MAX_MONITOR_LINES = 1000;

		private ArrayList m_logLines = new ArrayList(MAX_LOG_LINES);
		private ArrayList m_monitorLines = new ArrayList(MAX_MONITOR_LINES);
		private Runtime m_runtime = Runtime.Instance;
		private OpenAX25Settings m_settings = new OpenAX25Settings();
		private Control[] m_controlsSave = null;
        private StreamWriter m_logWriter = null;
        private StreamWriter m_monitorWriter = null;
		
		/// <summary>
		/// The Main Form of this application.
		/// </summary>
		public MainForm()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			SaveTabControls();

			//
			// Get application settings:
			//
			m_runtime.LogProvider = this;
			m_runtime.LogLevel = m_settings.LogLevel;
			if (m_settings.MonitorOn) {
				m_runtime.MonitorProvider = this;
				if (!mainTabControl.Controls.Contains(monitorTabPage))
					RestoreTabControls();
			} else {
				m_runtime.MonitorProvider = null;
				if (mainTabControl.Controls.Contains(monitorTabPage))
					mainTabControl.Controls.Remove(monitorTabPage);
			}
			this.configFileField.Text = m_settings.ConfigFile;

            this.logFileField.Text = m_settings.LogFile;
            if (!String.IsNullOrEmpty(this.logFileField.Text))
                OpenLogFile(this.logFileField.Text);

            this.monitorFileField.Text = m_settings.MonitorFile;
            if (!String.IsNullOrEmpty(this.monitorFileField.Text))
                OpenMonitorFile(this.monitorFileField.Text);

            //
			// Start program:
			//
			Runtime.Instance.Log(LogLevel.INFO, "MainForm", "Program started");
			
			// Load Config file:
            try
            {
                Runtime.Instance.LoadConfig(m_settings.ConfigFile);
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format(
                    "Config file \"{0}\" konnte nicht geladen werden ({1})!", m_settings.ConfigFile, e.Message));
            }
			
			logLevelNoneButton.Checked    = (m_runtime.LogLevel == LogLevel.NONE   );
			logLevelErrorButton.Checked   = (m_runtime.LogLevel == LogLevel.ERROR  );
			logLevelWarningButton.Checked = (m_runtime.LogLevel == LogLevel.WARNING);
			logLevelInfoButton.Checked    = (m_runtime.LogLevel == LogLevel.INFO   );
			logLevelDebugButton.Checked   = (m_runtime.LogLevel == LogLevel.DEBUG  );
			monitorCheckBox.Checked       = (m_runtime.MonitorProvider != null);
		}

		/// <summary>
		/// The logging implementation.
		/// </summary>
		/// <param name="component">Name of the component</param>
		/// <param name="message">Log message</param>
		public void OnLog(string component, string message)
		{
			string text = String.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}]: {2}",
                DateTime.UtcNow, component, message);
			AppendLog(text);
            if (m_logWriter != null)
            {
                try
                {
                    m_logWriter.WriteLine(text);
                    m_logWriter.Flush();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Unable to write to log file: " + ex.Message);
                    m_logWriter = null;
                }
            }
		}
		
		/// <summary>
		/// The monitor implementation.
		/// </summary>
		/// <param name="text">Monitor text</param>
		public void OnMonitor(string text)
		{
			AppendMonitor(text);
            if (m_monitorWriter != null)
            {
                try
                {
                    m_monitorWriter.WriteLine(text);
                    m_monitorWriter.Flush();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Unable to write to monitor file: " + ex.Message);
                    m_monitorWriter = null;
                }
            }
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
			logTextBox.AppendText(Environment.NewLine);
			logTextBox.ScrollToCaret();
		}

		private void AppendMonitor(string text)
		{
			if (InvokeRequired)
			{
				this.Invoke(new Action<string>(AppendMonitor), new object[] {text});
                return;
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
			monitorTextBox.AppendText(Environment.NewLine);
			monitorTextBox.ScrollToCaret();
		}
		
		private void SaveTabControls()
		{
			m_controlsSave = new Control[mainTabControl.Controls.Count];
			mainTabControl.Controls.CopyTo(m_controlsSave, 0);
		}
		
		private void RestoreTabControls()
		{
			TabPage tab = mainTabControl.SelectedTab;
			mainTabControl.Visible = false;
			mainTabControl.Controls.Clear();
			mainTabControl.Controls.AddRange(m_controlsSave);
			mainTabControl.SelectedTab = tab;
			mainTabControl.Visible = true;
		}
		
		void MainFormFormClosing(object sender, FormClosingEventArgs e)
		{
			m_settings.Save();
			e.Cancel = false;
			Application.Exit();
			Environment.Exit(0);
		}
		
		
		void LogLevelNoneButtonCheckedChanged(object sender, EventArgs e)
		{
			if (logLevelNoneButton.Checked) {
				m_runtime.LogLevel = LogLevel.NONE;
				m_settings.LogLevel = LogLevel.NONE;
			}
		}
		
		void LogLevelErrorButtonCheckedChanged(object sender, EventArgs e)
		{
			if (logLevelErrorButton.Checked) {
				m_runtime.LogLevel = LogLevel.ERROR;
				m_settings.LogLevel = LogLevel.ERROR;
			}
		}
		
		void LogLevelWarningButtonCheckedChanged(object sender, EventArgs e)
		{
			if (logLevelWarningButton.Checked) {
				m_runtime.LogLevel = LogLevel.WARNING;
				m_settings.LogLevel = LogLevel.WARNING;
			}
		}
		
		void LogLevelInfoButtonCheckedChanged(object sender, EventArgs e)
		{
			if (logLevelInfoButton.Checked) {
				m_runtime.LogLevel = LogLevel.INFO;
				m_settings.LogLevel = LogLevel.INFO;
			}
		}
		
		void LogLevelDebugButtonCheckedChanged(object sender, EventArgs e)
		{
			if (logLevelDebugButton.Checked) {
				m_runtime.LogLevel = LogLevel.DEBUG;
				m_settings.LogLevel = LogLevel.DEBUG;
			}
		}
		
		void MonitorCheckBoxCheckedChanged(object sender, EventArgs e)
		{
			if (monitorCheckBox.Checked) {
				m_runtime.MonitorProvider = this;
				m_settings.MonitorOn = true;
				if (!mainTabControl.Controls.Contains(monitorTabPage))
					RestoreTabControls();
			} else {
				m_runtime.MonitorProvider = null;
				m_settings.MonitorOn = false;
				if (mainTabControl.Controls.Contains(monitorTabPage))
					mainTabControl.Controls.Remove(monitorTabPage);
			}
		}
		
		
		void ConfigFileButtonClick(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			FileInfo fi = new FileInfo(configFileField.Text);
            ofd.Title = "Open Config File";
			ofd.InitialDirectory = fi.Directory.FullName;
			ofd.FileName = fi.Name;
			ofd.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
			ofd.FilterIndex = 1;
			ofd.Multiselect = false;
			ofd.RestoreDirectory = true;
			if (ofd.ShowDialog() == DialogResult.OK) {
				try {
					Stream st = ofd.OpenFile();
					if (st != null) {
						st.Close();
						configFileField.Text = ofd.FileName;
						m_settings.ConfigFile = ofd.FileName;
					} 
				} catch (Exception ex) {
					MessageBox.Show("Error: Could not read file: " + ex.Message);
				}
			}
		}

        private void LogFileButtonClick(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Choose Log File";
            sfd.AddExtension = true;
            sfd.AutoUpgradeEnabled = true;
            sfd.CheckFileExists = false;
            sfd.CheckPathExists = true;
            sfd.CreatePrompt = true;
            sfd.DefaultExt = ".log";
            sfd.DereferenceLinks = true;
            sfd.OverwritePrompt = false;
            sfd.ValidateNames = true;
            if (!String.IsNullOrEmpty(logFileField.Text))
            {
                FileInfo fi = new FileInfo(logFileField.Text);
                sfd.InitialDirectory = fi.Directory.FullName;
                sfd.FileName = fi.Name;
            }
            sfd.Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            sfd.FilterIndex = 1;
            sfd.RestoreDirectory = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                if (m_logWriter != null)
                {
                    try
                    {
                        m_logWriter.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: Could not close log file: " + ex.Message);
                    }
                }
                OpenLogFile(sfd.FileName);
                if (m_logWriter != null)
                {
                    logFileField.Text = sfd.FileName;
                    m_settings.LogFile = sfd.FileName;
                }
            }
        }

        private void OpenLogFile(string logFileName)
        {
            try
            {
                m_logWriter = File.AppendText(logFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Unable to open log file: " + ex.Message);
                m_logWriter = null;
            }
        }

        private void MonitorFileButtonClick(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Choose Monitor File";
            sfd.AddExtension = true;
            sfd.AutoUpgradeEnabled = true;
            sfd.CheckFileExists = false;
            sfd.CheckPathExists = true;
            sfd.CreatePrompt = true;
            sfd.DefaultExt = ".monitor";
            sfd.DereferenceLinks = true;
            sfd.OverwritePrompt = false;
            sfd.ValidateNames = true;
            if (!String.IsNullOrEmpty(monitorFileField.Text))
            {
                FileInfo fi = new FileInfo(monitorFileField.Text);
                sfd.InitialDirectory = fi.Directory.FullName;
                sfd.FileName = fi.Name;
            }
            sfd.Filter = "Monitor files (*.monitor)|*.monitor|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            sfd.FilterIndex = 1;
            sfd.RestoreDirectory = true;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                if (m_monitorWriter != null)
                {
                    try
                    {
                        m_monitorWriter.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: Could not close monitor file: " + ex.Message);
                    }
                }
                OpenMonitorFile(sfd.FileName);
                if (m_monitorWriter != null)
                {
                    monitorFileField.Text = sfd.FileName;
                    m_settings.MonitorFile = sfd.FileName;
                }
            }
        }

        private void OpenMonitorFile(string monitorFileName)
        {
            try
            {
                m_monitorWriter = File.AppendText(monitorFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Unable to open monitor file: " + ex.Message);
                m_monitorWriter = null;
            }
        }

    }
}
