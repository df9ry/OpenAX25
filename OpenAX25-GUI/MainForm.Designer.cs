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

namespace OpenAX25GUI
{
	partial class MainForm
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			this.mainTabControl = new System.Windows.Forms.TabControl();
			this.monitorTabPage = new System.Windows.Forms.TabPage();
			this.monitorTextBox = new System.Windows.Forms.TextBox();
			this.logTabPage = new System.Windows.Forms.TabPage();
			this.logTextBox = new System.Windows.Forms.TextBox();
			this.settingsTab = new System.Windows.Forms.TabPage();
			this.monitorCheckBox = new System.Windows.Forms.CheckBox();
			this.debugLevelGroup = new System.Windows.Forms.GroupBox();
			this.logLevelDebugButton = new System.Windows.Forms.RadioButton();
			this.logLevelInfoButton = new System.Windows.Forms.RadioButton();
			this.logLevelWarningButton = new System.Windows.Forms.RadioButton();
			this.logLevelErrorButton = new System.Windows.Forms.RadioButton();
			this.logLevelNoneButton = new System.Windows.Forms.RadioButton();
			this.label1 = new System.Windows.Forms.Label();
			this.configFileButton = new System.Windows.Forms.Button();
			this.configFileField = new System.Windows.Forms.TextBox();
			this.configFileLabel = new System.Windows.Forms.Label();
			this.mainTabControl.SuspendLayout();
			this.monitorTabPage.SuspendLayout();
			this.logTabPage.SuspendLayout();
			this.settingsTab.SuspendLayout();
			this.debugLevelGroup.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainTabControl
			// 
			this.mainTabControl.Controls.Add(this.monitorTabPage);
			this.mainTabControl.Controls.Add(this.logTabPage);
			this.mainTabControl.Controls.Add(this.settingsTab);
			this.mainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainTabControl.Location = new System.Drawing.Point(0, 0);
			this.mainTabControl.Name = "mainTabControl";
			this.mainTabControl.SelectedIndex = 0;
			this.mainTabControl.Size = new System.Drawing.Size(665, 413);
			this.mainTabControl.TabIndex = 0;
			// 
			// monitorTabPage
			// 
			this.monitorTabPage.Controls.Add(this.monitorTextBox);
			this.monitorTabPage.Location = new System.Drawing.Point(4, 22);
			this.monitorTabPage.Name = "monitorTabPage";
			this.monitorTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.monitorTabPage.Size = new System.Drawing.Size(657, 387);
			this.monitorTabPage.TabIndex = 3;
			this.monitorTabPage.Text = "Monitor";
			this.monitorTabPage.UseVisualStyleBackColor = true;
			// 
			// monitorTextBox
			// 
			this.monitorTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.monitorTextBox.Location = new System.Drawing.Point(3, 3);
			this.monitorTextBox.Multiline = true;
			this.monitorTextBox.Name = "monitorTextBox";
			this.monitorTextBox.ReadOnly = true;
			this.monitorTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.monitorTextBox.Size = new System.Drawing.Size(651, 381);
			this.monitorTextBox.TabIndex = 0;
			this.monitorTextBox.WordWrap = false;
			// 
			// logTabPage
			// 
			this.logTabPage.Controls.Add(this.logTextBox);
			this.logTabPage.Location = new System.Drawing.Point(4, 22);
			this.logTabPage.Name = "logTabPage";
			this.logTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.logTabPage.Size = new System.Drawing.Size(657, 387);
			this.logTabPage.TabIndex = 4;
			this.logTabPage.Text = "Log";
			this.logTabPage.UseVisualStyleBackColor = true;
			// 
			// logTextBox
			// 
			this.logTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.logTextBox.HideSelection = false;
			this.logTextBox.Location = new System.Drawing.Point(3, 3);
			this.logTextBox.Multiline = true;
			this.logTextBox.Name = "logTextBox";
			this.logTextBox.ReadOnly = true;
			this.logTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.logTextBox.Size = new System.Drawing.Size(651, 381);
			this.logTextBox.TabIndex = 0;
			this.logTextBox.WordWrap = false;
			// 
			// settingsTab
			// 
			this.settingsTab.Controls.Add(this.monitorCheckBox);
			this.settingsTab.Controls.Add(this.debugLevelGroup);
			this.settingsTab.Controls.Add(this.label1);
			this.settingsTab.Controls.Add(this.configFileButton);
			this.settingsTab.Controls.Add(this.configFileField);
			this.settingsTab.Controls.Add(this.configFileLabel);
			this.settingsTab.Location = new System.Drawing.Point(4, 22);
			this.settingsTab.Name = "settingsTab";
			this.settingsTab.Padding = new System.Windows.Forms.Padding(3);
			this.settingsTab.Size = new System.Drawing.Size(657, 387);
			this.settingsTab.TabIndex = 5;
			this.settingsTab.Text = "Settings";
			this.settingsTab.UseVisualStyleBackColor = true;
			// 
			// monitorCheckBox
			// 
			this.monitorCheckBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.monitorCheckBox.Location = new System.Drawing.Point(262, 76);
			this.monitorCheckBox.Name = "monitorCheckBox";
			this.monitorCheckBox.Size = new System.Drawing.Size(104, 24);
			this.monitorCheckBox.TabIndex = 5;
			this.monitorCheckBox.Text = "Monitor";
			this.monitorCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.monitorCheckBox.UseVisualStyleBackColor = true;
			this.monitorCheckBox.CheckedChanged += new System.EventHandler(this.MonitorCheckBoxCheckedChanged);
			// 
			// debugLevelGroup
			// 
			this.debugLevelGroup.Controls.Add(this.logLevelDebugButton);
			this.debugLevelGroup.Controls.Add(this.logLevelInfoButton);
			this.debugLevelGroup.Controls.Add(this.logLevelWarningButton);
			this.debugLevelGroup.Controls.Add(this.logLevelErrorButton);
			this.debugLevelGroup.Controls.Add(this.logLevelNoneButton);
			this.debugLevelGroup.Location = new System.Drawing.Point(119, 68);
			this.debugLevelGroup.Name = "debugLevelGroup";
			this.debugLevelGroup.Size = new System.Drawing.Size(128, 157);
			this.debugLevelGroup.TabIndex = 4;
			this.debugLevelGroup.TabStop = false;
			// 
			// logLevelDebugButton
			// 
			this.logLevelDebugButton.Location = new System.Drawing.Point(6, 127);
			this.logLevelDebugButton.Name = "logLevelDebugButton";
			this.logLevelDebugButton.Size = new System.Drawing.Size(104, 24);
			this.logLevelDebugButton.TabIndex = 4;
			this.logLevelDebugButton.TabStop = true;
			this.logLevelDebugButton.Text = "Debug";
			this.logLevelDebugButton.UseVisualStyleBackColor = true;
			this.logLevelDebugButton.CheckedChanged += new System.EventHandler(this.LogLevelDebugButtonCheckedChanged);
			// 
			// logLevelInfoButton
			// 
			this.logLevelInfoButton.Location = new System.Drawing.Point(6, 95);
			this.logLevelInfoButton.Name = "logLevelInfoButton";
			this.logLevelInfoButton.Size = new System.Drawing.Size(104, 24);
			this.logLevelInfoButton.TabIndex = 3;
			this.logLevelInfoButton.TabStop = true;
			this.logLevelInfoButton.Text = "Info";
			this.logLevelInfoButton.UseVisualStyleBackColor = true;
			this.logLevelInfoButton.CheckedChanged += new System.EventHandler(this.LogLevelInfoButtonCheckedChanged);
			// 
			// logLevelWarningButton
			// 
			this.logLevelWarningButton.Location = new System.Drawing.Point(6, 65);
			this.logLevelWarningButton.Name = "logLevelWarningButton";
			this.logLevelWarningButton.Size = new System.Drawing.Size(104, 24);
			this.logLevelWarningButton.TabIndex = 2;
			this.logLevelWarningButton.TabStop = true;
			this.logLevelWarningButton.Text = "Warning";
			this.logLevelWarningButton.UseVisualStyleBackColor = true;
			this.logLevelWarningButton.CheckedChanged += new System.EventHandler(this.LogLevelWarningButtonCheckedChanged);
			// 
			// logLevelErrorButton
			// 
			this.logLevelErrorButton.Location = new System.Drawing.Point(6, 35);
			this.logLevelErrorButton.Name = "logLevelErrorButton";
			this.logLevelErrorButton.Size = new System.Drawing.Size(104, 24);
			this.logLevelErrorButton.TabIndex = 1;
			this.logLevelErrorButton.TabStop = true;
			this.logLevelErrorButton.Text = "Error";
			this.logLevelErrorButton.UseVisualStyleBackColor = true;
			this.logLevelErrorButton.CheckedChanged += new System.EventHandler(this.LogLevelErrorButtonCheckedChanged);
			// 
			// logLevelNoneButton
			// 
			this.logLevelNoneButton.Location = new System.Drawing.Point(6, 5);
			this.logLevelNoneButton.Name = "logLevelNoneButton";
			this.logLevelNoneButton.Size = new System.Drawing.Size(104, 24);
			this.logLevelNoneButton.TabIndex = 0;
			this.logLevelNoneButton.TabStop = true;
			this.logLevelNoneButton.Text = "None";
			this.logLevelNoneButton.UseVisualStyleBackColor = true;
			this.logLevelNoneButton.CheckedChanged += new System.EventHandler(this.LogLevelNoneButtonCheckedChanged);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(22, 76);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(86, 19);
			this.label1.TabIndex = 3;
			this.label1.Text = "Log Level";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// configFileButton
			// 
			this.configFileButton.Location = new System.Drawing.Point(527, 27);
			this.configFileButton.Name = "configFileButton";
			this.configFileButton.Size = new System.Drawing.Size(33, 23);
			this.configFileButton.TabIndex = 2;
			this.configFileButton.Text = "...";
			this.configFileButton.UseVisualStyleBackColor = true;
			this.configFileButton.Click += new System.EventHandler(this.ConfigFileButtonClick);
			// 
			// configFileField
			// 
			this.configFileField.Location = new System.Drawing.Point(123, 29);
			this.configFileField.Name = "configFileField";
			this.configFileField.ReadOnly = true;
			this.configFileField.Size = new System.Drawing.Size(388, 20);
			this.configFileField.TabIndex = 1;
			// 
			// configFileLabel
			// 
			this.configFileLabel.Location = new System.Drawing.Point(17, 33);
			this.configFileLabel.Name = "configFileLabel";
			this.configFileLabel.Size = new System.Drawing.Size(91, 16);
			this.configFileLabel.TabIndex = 0;
			this.configFileLabel.Text = "Config File";
			this.configFileLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(665, 413);
			this.Controls.Add(this.mainTabControl);
			this.Name = "MainForm";
			this.Text = "Open AX.25 - True Open Source for the HamRadio Community - GNU LGPL V3 applies";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormFormClosing);
			this.mainTabControl.ResumeLayout(false);
			this.monitorTabPage.ResumeLayout(false);
			this.monitorTabPage.PerformLayout();
			this.logTabPage.ResumeLayout(false);
			this.logTabPage.PerformLayout();
			this.settingsTab.ResumeLayout(false);
			this.settingsTab.PerformLayout();
			this.debugLevelGroup.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		private System.Windows.Forms.CheckBox monitorCheckBox;
		private System.Windows.Forms.RadioButton logLevelInfoButton;
		private System.Windows.Forms.RadioButton logLevelDebugButton;
		private System.Windows.Forms.RadioButton logLevelErrorButton;
		private System.Windows.Forms.RadioButton logLevelWarningButton;
		private System.Windows.Forms.RadioButton logLevelNoneButton;
		private System.Windows.Forms.GroupBox debugLevelGroup;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button configFileButton;
		private System.Windows.Forms.Label configFileLabel;
		private System.Windows.Forms.TextBox configFileField;
		private System.Windows.Forms.TabPage settingsTab;
		private System.Windows.Forms.TextBox monitorTextBox;
		private System.Windows.Forms.TextBox logTextBox;
		private System.Windows.Forms.TabPage logTabPage;
		private System.Windows.Forms.TabPage monitorTabPage;
		private System.Windows.Forms.TabControl mainTabControl;
	}
}
