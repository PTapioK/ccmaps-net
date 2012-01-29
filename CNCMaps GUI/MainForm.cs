﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CNCMaps.GUI {

	public partial class MainForm : Form {
		public const string RendererExe = "CNCMaps.exe";

		public MainForm() {
			InitializeComponent();
		}

		private void MainFormLoad(object sender, EventArgs e) {
			tbRenderProg.Text = FindRenderProg();
			tbMixDir.Text = GetMixDir();
			UpdateCommandline();
			Height -= 180;
		}

		private string FindRenderProg() {
			try {
				RegistryKey k = Registry.LocalMachine.OpenSubKey("SOFTWARE\\CNC Map Render");
				if (k != null) {
					var s = (string) k.GetValue("");
					k.Close();
					return s + "\\" + RendererExe;
				}
			}
			catch (NullReferenceException) { } 
			return File.Exists(RendererExe) ? RendererExe : "";
		}

		private void OutputNameCheckedChanged(object sender, EventArgs e) {
			tbCustomOutput.Visible = rbCustomFilename.Checked;
			UpdateCommandline();
		}

		private void BrowseMixDir(object sender, EventArgs e) {
			folderBrowserDialog1.Description = "The directory that contains the mix files for RA2/YR.";
			folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
			folderBrowserDialog1.SelectedPath = GetMixDir();
			folderBrowserDialog1.ShowNewFolderButton = false;
			if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
				tbMixDir.Text = folderBrowserDialog1.SelectedPath;
		}

		private void BrowseRenderer(object sender, EventArgs e) {
			ofd.CheckFileExists = true;
			ofd.Multiselect = false;
			ofd.Filter = "Executable (*.exe)|*.exe";
			ofd.InitialDirectory = Directory.GetCurrentDirectory();
			ofd.FileName = "cncmaprender.exe";
			if (ofd.ShowDialog() == DialogResult.OK) {
				tbRenderProg.Text = ofd.FileName.StartsWith(Directory.GetCurrentDirectory()) ?
					ofd.FileName.Substring(Directory.GetCurrentDirectory().Length + 1) :
					ofd.FileName;
			}
		}

		private void InputDragEnter(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.Move;
		}

		private void InputDragDrop(object sender, DragEventArgs e) {
			var files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files.Length > 0) {
				tbInput.Text = files[0];
				UpdateCommandline();
			}
		}

		private void UpdateCommandline() {
			string cmd = GetCommandline();
			string file = tbRenderProg.Text;
			if (file.Contains("\\"))
				file = file.Substring(file.LastIndexOf('\\') + 1);
			tbCommandPreview.Text = file + " " + cmd;
		}

		private string GetCommandline() {
			string cmd = string.Empty;

			cmd += "-i \"" + tbInput.Text + "\" ";
			if (cbOutputPNG.Checked) {
				cmd += "-p ";
				if (nudCompression.Value != 6)
					cmd += "-c " + nudCompression.Value.ToString(CultureInfo.InvariantCulture) + " ";
			}

			if (rbCustomFilename.Checked) cmd += "-o \"" + tbCustomOutput.Text + "\" ";
			if (cbOutputJPG.Checked) {
				cmd += "-j ";
				if (nudEncodingQuality.Value != 90)
					cmd += "-q " + nudEncodingQuality.Value.ToString(CultureInfo.InvariantCulture) + " ";
			}

			if (tbMixDir.Text != GetMixDir()) cmd += "-m " + "\"" + tbMixDir.Text + "\" ";
			if (cbEmphasizeOre.Checked) cmd += "-r ";
			if (cbTiledStartPositions.Checked) cmd += "-s ";
			if (cbSquaredStartPositions.Checked) cmd += "-S ";
			if (rbEngineYR.Checked) cmd += "-Y ";
			else if (rbEngineRA2.Checked) cmd += "-y ";
			if (rbSizeFullmap.Checked) cmd += "-f ";
			if (rbSizeFullmap.Checked) cmd += "-F ";
			//if (cbSoftwareRendering.Checked) cmd += "-g ";
			if (cbReplacePreview.Checked) cmd += "-k ";

			return cmd;
		}

		private static string GetMixDir() {
			try {
				RegistryKey k = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Westwood\\Red Alert 2");
				if (k != null) {
					var s = (string) k.GetValue("InstallPath");
					k.Close();
					return s.Substring(0, s.LastIndexOf('\\'));
				}
			}
			catch (NullReferenceException) {}
			return "";
		}

		private void UIChanged(object sender, EventArgs e) { UpdateCommandline(); }

		private void PngOutputCheckedChanged(object sender, EventArgs e) {
			nudCompression.Visible = label1.Visible = cbOutputPNG.Checked;
			UpdateCommandline();
		}

		private void JpegOutputCheckedChanged(object sender, EventArgs e) {
			lblQuality.Visible = nudEncodingQuality.Visible = cbOutputJPG.Checked;
			UpdateCommandline();
		}

		private void BrowseInput(object sender, EventArgs e) {
			ofd.CheckFileExists = true;
			ofd.Multiselect = false;
			ofd.Filter = "RA2/YR map files (*.map, *.mpr, *.mmx, *.yrm, *.yro)|*.mpr;*.map;*.mmx;*.yrm;*.yro|All files (*.*)|*";
			ofd.InitialDirectory = GetMixDir();
			ofd.FileName = "";
			if (ofd.ShowDialog() == DialogResult.OK)
				tbInput.Text = ofd.FileName;
		}

		private void SquaredStartPosCheckedChanged(object sender, EventArgs e) {
			if (cbSquaredStartPositions.Checked)
				cbTiledStartPositions.Checked = false;
			UpdateCommandline();
		}

		private void TiledStartPosCheckedChanged(object sender, EventArgs e) {
			if (cbTiledStartPositions.Checked)
				cbSquaredStartPositions.Checked = false;
			UpdateCommandline();
		}

		private void ExecuteCommand(object sender, EventArgs e) {
			if (File.Exists(tbInput.Text) == false) {
				MessageBox.Show("Input file doesn't exist. Aborting.");
				return;
			}

			if (File.Exists(tbMixDir.Text + "\\ra2.mix") == false) {
				MessageBox.Show("File ra2.mix not found. Aborting.");
				return;
			}

			string exepath = tbRenderProg.Text;
			if (File.Exists(exepath) == false) {
				exepath = Application.ExecutablePath;
				if (exepath.Contains("\\"))
					exepath = exepath.Substring(0, exepath.LastIndexOf('\\') + 1);
				exepath += "cncmaprender.exe";
				if (File.Exists(exepath) == false) {
					MessageBox.Show("File cncmaprender.exe not found. Aborting.");
					return;
				}
			}

			if (!cbOutputPNG.Checked && !cbOutputJPG.Checked) {
				MessageBox.Show("No output format chosen. Aborting.");
				return;
			}

			MakeLog();
			ProcessCmd(exepath);
		}


		private void ProcessCmd(string exepath) {
			try {
				var p = new Process { StartInfo = { FileName = exepath, Arguments = GetCommandline() } };

				p.OutputDataReceived += ConsoleDataReceived;
				p.StartInfo.CreateNoWindow = true;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.UseShellExecute = false;
				p.Start();
				p.BeginOutputReadLine();
			}
			catch (InvalidOperationException) { }
			catch (Win32Exception) { }
		}

		#region Logging

		private void ConsoleDataReceived(object sender, DataReceivedEventArgs e) {
			if (e.Data == null) {
				// indicates EOF
				Log("\r\nYour map has been rendered. If your image did not appear, something went wrong." +
					" Please sent an email to frank@zzattack.org with your map as an attachment.");
			}
			else {
				Log(e.Data);
			}
		}

		bool _showlog;
		private void MakeLog() {
			if (_showlog)
				return;

			Height += 180;
			cbLog.Visible = true;
			_showlog = true;
		}

		private delegate void LogDelegate(string s);

		private void Log(string s) {
			if (InvokeRequired) {
				Invoke(new LogDelegate(Log), s);
				return;
			}
			rtbLog.Text += s + "\r\n";
			rtbLog.SelectionStart = rtbLog.TextLength - 1;
			rtbLog.SelectionLength = 1;
			rtbLog.ScrollToCaret();
		}

		#endregion

	}
}