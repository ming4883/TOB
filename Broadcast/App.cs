////css_co /unsafe;
//css_import Package
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace TOB
{
	class App
	{
		static string VLC_PATH = Path.GetFullPath (@".\vlc\vlc.exe");
		
		class UI
		{
			static public int IP1 = 192;
			static public int IP2 = 168;
			static public int IP3 = 1;
			static public int IP4 = 112;
			static public string IP
			{
				get
				{
					return string.Format ("{0}.{1}.{2}.{3}", IP1, IP2, IP3, IP4);
				}
			}
			
			static public bool FULLSCREEN = true;
			
			static public void Init (string[] args)
			{
				var frm = new Form() {
					Text = "TobChurch Broadcast",
					Size = new Size (480, 240),
					Padding = new Padding (5),
					Font = new Font ("Consolas", 12.0f),
					FormBorderStyle = FormBorderStyle.FixedSingle,
					MaximizeBox = false,
					Icon = Icon.ExtractAssociatedIcon(VLC_PATH),
				};
				
				frm.SuspendLayout();
				
				{
					GroupBox gb = new GroupBox() {
						Dock = DockStyle.Top,
						Height = 80,
						Text = "Broadcast",
					};
					frm.Controls.Add (gb);
					#if false
					{
						Button bt = new Button() {
							Dock = DockStyle.Left,
							Text = "CONTROL",
							Width = 100,
						};
						bt.Click += (s, e) => {
						};
						gb.Controls.Add (bt);
					}
					#endif
					{
						Button bt = new Button() {
							Dock = DockStyle.Left,
							Text = "STOP",
							Width = 100,
						};
						bt.Click += (s, e) => {
							Streaming.Stop();
						};
						gb.Controls.Add (bt);
					}
					{
						Button bt = new Button() {
							Dock = DockStyle.Left,
							Text = "START",
							Width = 100,
						};
						bt.Click += (s, e) => {
							Streaming.Start (IP, FULLSCREEN);
						};
						gb.Controls.Add (bt);
					}
				}
				
				{
					GroupBox gb = new GroupBox() {
						Dock = DockStyle.Top,
						Height = 120,
						Text = "Options",
					};
					frm.Controls.Add (gb);
					
					{
						Panel p = new Panel() {
							Dock = DockStyle.Top,
							Height = 35,
						};
						gb.Controls.Add (p);
						{
							NumericUpDown  ud = new NumericUpDown() {
								Dock = DockStyle.Left,
								Maximum = 254,
								Minimum = 2,
								Value = IP4,
								Width = 60,
							};
							p.Controls.Add (ud);
							ud.ValueChanged += (s, e) =>
							{
								IP4 = (int)ud.Value;
							};
							
						}
						{
							Label la = new Label() {
								Text = ".",
								Dock = DockStyle.Left,
								TextAlign = ContentAlignment.MiddleLeft,
								AutoSize = true,
							};
							p.Controls.Add (la);
						}
						{
							NumericUpDown  ud = new NumericUpDown() {
								Dock = DockStyle.Left,
								Maximum = 255,
								Minimum = 1,
								Value = IP3,
								Width = 60,
							};
							ud.ValueChanged += (s, e) =>
							{
								IP3 = (int)ud.Value;
							};
							p.Controls.Add (ud);
						}
						{
							Label la = new Label() {
								Text = ".",
								Dock = DockStyle.Left,
								TextAlign = ContentAlignment.MiddleLeft,
								AutoSize = true,
							};
							p.Controls.Add (la);
						}
						{
							NumericUpDown  ud = new NumericUpDown() {
								Dock = DockStyle.Left,
								Maximum = 255,
								Minimum = 1,
								Value = IP2,
								Width = 60,
							};
							p.Controls.Add (ud);
							ud.ValueChanged += (s, e) =>
							{
								IP2 = (int)ud.Value;
							};
						}
						{
							Label la = new Label() {
								Text = ".",
								Dock = DockStyle.Left,
								TextAlign = ContentAlignment.MiddleLeft,
								AutoSize = true,
							};
							p.Controls.Add (la);
						}
						{
							NumericUpDown  ud = new NumericUpDown() {
								Dock = DockStyle.Left,
								Maximum = 255,
								Minimum = 1,
								Value = IP1,
								Width = 60,
							};
							ud.ValueChanged += (s, e) =>
							{
								IP1 = (int)ud.Value;
							};
							p.Controls.Add (ud);
						}
						{
							Label la = new Label() {
								Text = "IP ",
								Dock = DockStyle.Left,
								TextAlign = ContentAlignment.MiddleLeft,
								AutoSize = true,
							};
							p.Controls.Add (la);
						}
					}
					
					{
						CheckBox ck = new CheckBox() {
							Dock = DockStyle.Top,
							Text = "Fullscreen",
							Checked = FULLSCREEN,
							Height = 35,
							//CheckAlign = ContentAlignment.MiddleRight,
						};
						gb.Controls.Add (ck);
						ck.CheckedChanged += (s, e) =>
						{
							FULLSCREEN = ck.Checked;
							//Console.WriteLine(FULLSCREEN);
						};
						
					}
					
				}
				
				frm.ResumeLayout();
				
				Application.Run (frm);
			}
		}
		
		class Streaming
		{
		// https://wiki.videolan.org/VLC_command-line_help
			const string CACHING_OPTION = "--network-caching=20";
			const string MINIMIZED = "--qt-start-minimized";
			const string FULLSCREEN = "--fullscreen";
			
			
			static public void Init (string[] args)
			{
				
				string ret = Package.Extract (Path.GetFullPath ("./vlc.zip"), Path.GetFullPath ("./vlc/"));
				
				if (!string.IsNullOrWhiteSpace (ret))
					Console.WriteLine ("Extraction skipped: {0}", ret);
			}
			
			static public void Start(string ip, bool fullscreen)
			{
				Console.WriteLine("streaming ip: {0}", ip);
				StartVLC(string.Format ("http://{0}:8080/audio.wav", ip), 
					new string[] {
						"-vv",
						"--no-video",
						CACHING_OPTION,
						MINIMIZED,
					});
				StartVLC(string.Format ("http://{0}:8080/video", ip), 
					new string[] {
						"-vv",
						"--no-audio",
						CACHING_OPTION,
						fullscreen ? FULLSCREEN : "",
					});
			}
			
			static public void Stop()
			{
				int killed = KillAllProcess ("vlc");
			
				if (killed > 0)
				{
					Console.WriteLine ("killed {0} deprecated vlc process", killed);
				}
			}
			
			static Process StartVLC (string url, string[] args)
			{
				Process p = new Process();
				StringBuilder sb = new StringBuilder();
				
				if (args != null)
				{
					foreach (var a in args)
					{
						sb.Append (a);
						sb.Append (" ");
					}
				}
				
				sb.Append (url); sb.Append (" ");
				
				try
				{
					//Console.WriteLine (sb);
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.FileName = VLC_PATH;
					p.StartInfo.Arguments = sb.ToString();
					p.Start();
				}
				catch (Exception e)
				{
					Console.WriteLine (e);
					return null;
				}
				
				return p;
			}
			
			static int KillAllProcess (string imgName)
			{
				int cnt = 0;
				foreach (Process p in Process.GetProcessesByName(imgName))
				{
					p.Kill();
					p.WaitForExit (1000);
					++cnt;
				}
				return cnt;
			}
		}
		
		static public void Main (string[] args)
		{
			Streaming.Init (args);
			UI.Init (args);
		}
	}	// class App

}	// namespace TOB
