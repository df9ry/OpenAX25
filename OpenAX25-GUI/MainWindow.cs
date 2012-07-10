//
// Main.cs
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
using Gtk;
using OpenAX25KISS;
using System.Threading;
using OpenAX25GUI;

public partial class MainWindow: Gtk.Window
{	

	readonly char[] HEX = {'0','1','2','3','4','5','6','7','8','9','a','b','c','d','e','f'};

	L2_KISS kissInterface = null;
	Thread receiveThread = null;
	Thread transmitThread = null;

	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
		shellTextView.Buffer.InsertAtCursor("=> ");

		this.kissInterface = new L2_KISS("KISS", "COM12", 9600);
		receiveThread = new Thread( new ThreadStart(this.Receive));
		receiveThread.Start ();
		transmitThread = new Thread( new ThreadStart(this.Transmit));
		transmitThread.Start ();
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		if (receiveThread != null)
			receiveThread.Abort();
		if (transmitThread != null)
			transmitThread.Abort();
		Application.Quit ();
		a.RetVal = true;
	}

	protected void OnShellTextViewKeyReleaseEvent (object o, KeyReleaseEventArgs args)
	{
		Gdk.Key key = args.Event.Key;
		switch (key) {
		case Gdk.Key.Return :
			shellTextView.Buffer.InsertAtCursor("=> ");
			break;
		default :
			break;
		} // end switch //
	}

	void Monitor (string text)
	{
		/*
		TextBuffer buffer = this.monitorView.Buffer;
		lock (buffer) {
			while (buffer.CharCount > 1024) {
				TextIter start = buffer.GetIterAtOffset(0);
				TextIter end = buffer.GetIterAtLine(1);
				buffer.Delete(ref start, ref end);
			} // end while //
			TextIter endIter = buffer.EndIter;
			buffer.Insert(endIter, text + Environment.NewLine);
		}
		*/
		Console.WriteLine(text);
	}

	void Receive ()
	{
		Monitor ("Thread started");
		while (true) {
			byte[] frame = kissInterface.ReceiveFrame(true);
			if (frame != null) {
				Monitor(kissInterface.Name + " <- ", frame);
			} else {
				Monitor(kissInterface.Name + ": null");
			}
		} // end while //
	}

	void Transmit ()
	{
		while (true) {
			Thread.Sleep (2000);
			byte[] frame = SamplePacket.Frame;
			Monitor (kissInterface.Name + " -> ", frame);
			kissInterface.SendFrame(frame, true, false);
		}

	}

	private void Monitor (string line, byte[] data)
	{
		foreach (byte b in data) {
			line += HEX[b/16];
			line += HEX[b%16];
			line += ' ';
		} // end foreach //
		Monitor(line);
	}

}
