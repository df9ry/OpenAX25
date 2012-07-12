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
			this.shellTabPage = new System.Windows.Forms.TabPage();
			this.monitorTabPage = new System.Windows.Forms.TabPage();
			this.monitorTextBox = new System.Windows.Forms.TextBox();
			this.logTabPage = new System.Windows.Forms.TabPage();
			this.logTextBox = new System.Windows.Forms.TextBox();
			this.mainTabControl.SuspendLayout();
			this.monitorTabPage.SuspendLayout();
			this.logTabPage.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainTabControl
			// 
			this.mainTabControl.Controls.Add(this.shellTabPage);
			this.mainTabControl.Controls.Add(this.monitorTabPage);
			this.mainTabControl.Controls.Add(this.logTabPage);
			this.mainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainTabControl.Location = new System.Drawing.Point(0, 0);
			this.mainTabControl.Name = "mainTabControl";
			this.mainTabControl.SelectedIndex = 0;
			this.mainTabControl.Size = new System.Drawing.Size(665, 413);
			this.mainTabControl.TabIndex = 0;
			// 
			// shellTabPage
			// 
			this.shellTabPage.Location = new System.Drawing.Point(4, 22);
			this.shellTabPage.Name = "shellTabPage";
			this.shellTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.shellTabPage.Size = new System.Drawing.Size(657, 387);
			this.shellTabPage.TabIndex = 2;
			this.shellTabPage.Text = "Shell";
			this.shellTabPage.UseVisualStyleBackColor = true;
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
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(665, 413);
			this.Controls.Add(this.mainTabControl);
			this.Name = "MainForm";
			this.Text = "Open AX.25";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormFormClosing);
			this.mainTabControl.ResumeLayout(false);
			this.monitorTabPage.ResumeLayout(false);
			this.monitorTabPage.PerformLayout();
			this.logTabPage.ResumeLayout(false);
			this.logTabPage.PerformLayout();
			this.ResumeLayout(false);
		}
		private System.Windows.Forms.TextBox monitorTextBox;
		private System.Windows.Forms.TextBox logTextBox;
		private System.Windows.Forms.TabPage logTabPage;
		private System.Windows.Forms.TabPage monitorTabPage;
		private System.Windows.Forms.TabPage shellTabPage;
		private System.Windows.Forms.TabControl mainTabControl;
	}
}
