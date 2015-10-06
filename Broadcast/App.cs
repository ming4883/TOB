////css_co /unsafe;
//css_import Package
//css_import VLC
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
		static string VLC_PATH = Path.GetFullPath (@".\vlc\x86\vlc.exe");
		
		class Log
		{
			static public void WriteLine(object a)
			{
				if (Environment.UserInteractive)
					Console.WriteLine(a);
			}
			
			static public void WriteLine(string fmt, object a)
			{
				if (Environment.UserInteractive)
					Console.WriteLine(fmt, a);
			}
			
			static public void WriteLine(string fmt, object a, object b)
			{
				if (Environment.UserInteractive)
					Console.WriteLine(fmt, a, b);
			}
			
			static public void WriteLine(string fmt, object a, object b, object c)
			{
				if (Environment.UserInteractive)
					Console.WriteLine(fmt, a, b, c);
			}
			
			static public void WriteLine(string fmt, object a, object b, object c, object d)
			{
				if (Environment.UserInteractive)
					Console.WriteLine(fmt, a, b, c, d);
			}
		}
		
		class Streaming
		{
			// https://wiki.videolan.org/VLC_command-line_help
			const string AUDIO_CACHING_OPTION = "--network-caching=1536";
			const string VIDEO_CACHING_OPTION = "--network-caching=1024";
			const int VIDEO_QUALITY = 85;
			
			static Object _Sync = new Object();
			static bool _Playing = false;
			static Thread _Thread = null;
			static VLC _vlc = null;
			static VLC.MediaPlayback _VideoProc = null;
			static VLC.MediaPlayback _AudioProc = null;
			static DateTime _LastSync;
			
			static public void Init (string[] args)
			{
				string ret = Package.Extract (Path.GetFullPath ("./vlc.zip"), Path.GetFullPath ("./vlc/x86"));
				
				if (!string.IsNullOrWhiteSpace (ret))
					Log.WriteLine ("Extraction skipped: {0}", ret);
				
				_vlc = new VLC (Path.GetFullPath ("./vlc"));
			}
			
			static public void Quit()
			{
				Stop();
				_vlc.Dispose();
			}
			
			static public bool Start(string ip, bool fullscreen)
			{
				lock (_Sync)
				{
					Log.WriteLine("Streaming from {0}", ip);
					
					StartAudioAndVideoPlayback (ip, fullscreen);
					
					if (!SetQuality(ip, VIDEO_QUALITY))
					{
						UI.Warn(string.Format("Cannot connect to {0}", ip));
						
						if (null != _AudioProc)
						{
							_AudioProc.Stop();
							_AudioProc.Dispose();
							_AudioProc = null;
						}
						
						if (null != _VideoProc)
						{
							_VideoProc.Stop();
							_VideoProc.Dispose();
							_VideoProc = null;
						}
						return false;
					}
					
					_LastSync = DateTime.Now;
					
					_Playing = true;
					
					return true;
				}
			}
			
			static bool RestartIfNeeded()
			{
				int retry = 0;
				const int MAX_RETRY = 3;
				
				//Log.WriteLine("audio:{0} video:{1}", _AudioProc.State, _VideoProc.State);
				
				for (int i = 0; i < MAX_RETRY; ++i)
				{
					if (!_AudioProc.Alive || !_VideoProc.Alive)
						retry++;
					else
						break;
					
					Thread.Sleep (1000);
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
					lock(_Sync)
					{
						bool fullscreen = _VideoProc.Fullscreen;
						
						_AudioProc.Stop();
						_VideoProc.Stop();
						
						Thread.Sleep (1000);
						
						_AudioProc.Play();
						_VideoProc.Play();
						_AudioProc.SetVolume (100);
						_VideoProc.Fullscreen = fullscreen;
						_LastSync = DateTime.Now;
					}
					
					return false;
				}
				else
				{
					UI.SetStatus ("Playing");
					return true;
				}
			}
			
			static public void SyncAudio()
			{
				lock(_Sync)
				{
					if (!_AudioProc.Alive || !_VideoProc.Alive)
						return;
					
					var time = DateTime.Now.Subtract (_LastSync);
					if (time.Minutes >= 15)
					{
						_AudioProc.Stop();
						_AudioProc.Play();
						_LastSync = DateTime.Now;
						Log.WriteLine("SyncAudio at {0}", _LastSync);
					}
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
						
						SyncAudio();
						
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
					if (null != _AudioProc)
					{
						_AudioProc.Stop();
						_AudioProc.Dispose();
						_AudioProc = null;
					}
					
					if (null != _VideoProc)
					{
						_VideoProc.Stop();
						_VideoProc.Dispose();
						_VideoProc = null;
					}
				}
			}
			
			static bool SetQuality(string ip, int value)
			{
				const int MAX_RETRY = 3;
				
				for (int i=0; i <MAX_RETRY; ++i)
				{
					try
					{
						string url = string.Format ("http://{0}:8080/settings/quality?set={1}", ip, value);
						var request = WebRequest.Create(url) as HttpWebRequest;
						request.Timeout = 10000;
						
						var response = request.GetResponse() as HttpWebResponse;
						XmlDocument xmlDoc = new XmlDocument();
						xmlDoc.Load(response.GetResponseStream());
						response.Close();
						
						string ret = xmlDoc.GetElementsByTagName("result")[0].InnerText.ToLower();
						if (ret == "ok")
							return true;
					}
					catch(Exception err)
					{
						Log.WriteLine("SetQuality pass {0} failed, {1}", i, err);
						Thread.Sleep (500);
					}
				}
				
				return false;
			}
			
			static void StartAudioAndVideoPlayback(string ip, bool fullscreen)
			{
				_AudioProc = _vlc.CreatePlayback(
					string.Format ("http://{0}:8080/audio.wav", ip), 
					new string[] {
						//"--no-video",
						AUDIO_CACHING_OPTION,
					});
				_AudioProc.Play();
				
				_VideoProc = _vlc.CreatePlayback(
					string.Format ("http://{0}:8080/video4flash", ip), 
					new string[] {
						//"--no-audio",
						VIDEO_CACHING_OPTION,
					});
				_VideoProc.Play();
				
				_AudioProc.SetVolume (100);
				_VideoProc.Fullscreen = fullscreen;
			}
		}
		
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
					if (_frm.WindowState == FormWindowState.Minimized)
						_frm.WindowState = FormWindowState.Normal;
					
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
		
		static public void Main (string[] args)
		{
			Streaming.Init (args);
			
			UI.Init (args);
			
			Streaming.Quit();
		}
	}	// class App

}	// namespace TOB
