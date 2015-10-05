////css_co /unsafe;
//css_import Package
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Net;
using System.Xml;

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
			
			static GroupBox _gb;
			static Form _frm;
			
			static public void SetStatus(string text)
			{
				var task = new MethodInvoker(()=>
				{
					if (string.IsNullOrWhiteSpace(text))
						_gb.Text = "Broadcast";
					else
						_gb.Text = "Broadcast - " + text;
				});
				
				if (_gb.InvokeRequired)
					_gb.Invoke(task);
				else
					task();
			}
			
			static public void Focus()
			{
				var task = new MethodInvoker(()=>
				{
					_frm.Activate();
				});
				
				if (_frm.InvokeRequired)
					_frm.Invoke(task);
				else
					task();
			}
			
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
					_gb = gb;
					{
						Button bt = new Button() {
							Dock = DockStyle.Left,
							Text = "STOP",
							Width = 100,
						};
						bt.Click += (s, e) => {
							Streaming.Stop();
							SetStatus("");
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
							Streaming.Stop();
							if (Streaming.Start (IP, FULLSCREEN))
							{
								Streaming.EnableAutoRestart();
								SetStatus("Playing");
							}
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
								Maximum = 255,
								Minimum = 0,
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
								Minimum = 0,
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
								Minimum = 0,
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
								Minimum = 0,
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
				_frm = frm;
				
				Application.Run (frm);
			}
			
			static public void Warn(string msg)
			{
				MessageBox.Show(msg);
			}
		}
		
		class Streaming
		{
			// https://wiki.videolan.org/VLC_command-line_help
			const string AUDIO_CACHING_OPTION = "--network-caching=1536";
			const string VIDEO_CACHING_OPTION = "--network-caching=1536";
			const string MINIMIZED = "--qt-start-minimized";
			const string FULLSCREEN = "--fullscreen";
			const string NO_SYSTEM_TRAY = "--no-qt-system-tray";
			const int VIDEO_QUALITY = 85;
			
			static Process _VideoProc = null;
			static Process _AudioProc = null;
			static Object _Sync = new Object();
			static string _LastIP = "";
			static bool _LastFullscreen = false;
			static bool _Playing = false;
			static Thread _Thread = null;
			
			static public void Init (string[] args)
			{
				string ret = Package.Extract (Path.GetFullPath ("./vlc.zip"), Path.GetFullPath ("./vlc/"));
				
				if (!string.IsNullOrWhiteSpace (ret))
					Console.WriteLine ("Extraction skipped: {0}", ret);
			}
			
			static public bool Start(string ip, bool fullscreen)
			{
				lock (_Sync)
				{
					Console.WriteLine("streaming ip: {0}", ip);
					if (!SetQuality(ip, VIDEO_QUALITY))
					{
						UI.Warn(string.Format("Cannot connect to {0}", ip));
						return false;
					}
					
					StartAudioAndVideoProcess (ip, fullscreen);
					
					_LastIP = ip;
					_LastFullscreen = fullscreen;
					_Playing = true;
					
					return true;
				}
			}
			
			static void RestartIfNeeded()
			{
				int retry = 0;
				const int MAX_RETRY = 3;
				
				for (int i = 0; i < MAX_RETRY; ++i)
				{
					try
					{
						string url = string.Format ("http://{0}:8080/settings/quality?set={1}", _LastIP, VIDEO_QUALITY);
						var request = WebRequest.Create(url) as HttpWebRequest;
						request.Timeout = 5000;
						var response = request.GetResponse() as HttpWebResponse;
						var code = response.StatusCode;
						response.Close();
						if (code == HttpStatusCode.OK)
							break;
						
						retry++;
					}
					catch(Exception)
					{
						retry++;
					}
				}
				
				if (retry >= MAX_RETRY)
				{
					UI.Focus();
					
					// wait for 10 sec and then auto restart
					for(int i = 10; i > 0; --i)
					{
						UI.SetStatus (string.Format("Interrupted, reconnect in {0}", i));
						Thread.Sleep (1000);
					}
					
					UI.SetStatus ("Reconnecting");
					Kill (_AudioProc);
					Kill (_VideoProc);
					
					StartAudioAndVideoProcess (_LastIP, _LastFullscreen);
				}
				else
				{
					UI.SetStatus ("Playing");
				}
			}
			
			static public void EnableAutoRestart()
			{
				Thread t = new Thread(new ThreadStart(()=>
				{
					bool playing;
					lock(_Sync)
					{
						playing = _Playing;
					}
					
					while (playing)
					{
						Thread.Sleep(5000);
						
						RestartIfNeeded();
						
						lock(_Sync)
						{
							playing = _Playing;
						}
					}
				}));
				
				t.Start();
				_Thread = t;
			}
			
			static public void Stop()
			{
				lock (_Sync)
				{
					_Playing = false;
				}
				
				lock (_Sync)
				{
					if (null != _Thread)
					{
						_Thread.Abort();
						_Thread = null;
						
					}
					int killed = 0;
					
					if (Kill (_AudioProc))
						killed++;
					_AudioProc = null;
					
					if (Kill (_VideoProc))
						killed++;
					_VideoProc = null;
					
					killed += KillAllProcess ("vlc");
				
					if (killed > 2)
					{
						Console.WriteLine ("killed {0} deprecated vlc process", killed);
					}
				}
			}
			
			static bool SetQuality(string ip, int value)
			{
				try
				{
					string url = string.Format ("http://{0}:8080/settings/quality?set={1}", ip, value);
					var request = WebRequest.Create(url) as HttpWebRequest;
					request.Timeout = 2000;
					
					var response = request.GetResponse() as HttpWebResponse;
					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.Load(response.GetResponseStream());
					string ret = xmlDoc.GetElementsByTagName("result")[0].InnerText.ToLower();
					response.Close();
					return ret == "ok";
				}
				catch(Exception )
				{
					return false;
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
				
				Thread.Sleep (1000);
				
				return p;
			}
			
			static void StartAudioAndVideoProcess(string ip, bool fullscreen)
			{
				_AudioProc = StartVLC(string.Format ("http://{0}:8080/audio.wav", ip), 
					new string[] {
						"-vv",
						//"--no-video",
						AUDIO_CACHING_OPTION,
						NO_SYSTEM_TRAY,
						//MINIMIZED,
					});
				_AudioProc.PriorityClass = ProcessPriorityClass.High;
				
				_VideoProc = StartVLC(string.Format ("http://{0}:8080/video4flash", ip), 
					new string[] {
						"-vv",
						//"--no-audio",
						VIDEO_CACHING_OPTION,
						NO_SYSTEM_TRAY,
						fullscreen ? FULLSCREEN : "",
					});
				_VideoProc.PriorityClass = ProcessPriorityClass.High;
			}
			
			static bool Kill(Process p)
			{
				if (null != p && !p.HasExited)
				{
					p.Kill();
					p.WaitForExit (1000);
					return true;
				}
				
				return false;
			}
			
			static int KillAllProcess (string imgName)
			{
				int cnt = 0;
				foreach (Process p in Process.GetProcessesByName(imgName))
				{
					if (Kill (p))
						++cnt;
				}
				return cnt;
			}
		}
		
		static public void Main (string[] args)
		{
			Streaming.Init (args);
			
			UI.Init (args);
			
			Streaming.Stop();
		}
	}	// class App

}	// namespace TOB
