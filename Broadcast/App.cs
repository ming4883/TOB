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
		public static bool DevMode = false;
		
		class Streaming
		{
			// https://wiki.videolan.org/VLC_command-line_help
			const string CACHING = "2048";
			const string FILE_CACHING_OPTION = "--file-caching=" + CACHING;
			const string LIVE_CACHING_OPTION = "--live-caching=" + CACHING;
			const string DISK_CACHING_OPTION = "--disk-caching=" + CACHING;
			const string NETWORK_CACHING_OPTION = "--network-caching=" + CACHING;
			
			static Object _Sync = new Object();
			static bool _Playing = false;
			static Thread _Thread = null;
			static VLC _vlc = null;
			static VLC.MediaPlayback _VideoProc = null;
			static VLC.MediaPlayback _AudioProc = null;
			static string _IP = null;
			static DateTime _LastSync;
			static int SYNC_INTERVAL = 5;
			
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
					StartAudioAndVideoPlayback (ip, fullscreen);
					
					if (!IsRemoteAlive())
						return false;
					
					_IP = ip;
					_LastSync = DateTime.Now;
					
					Log.WriteLine("Streaming from {0} at {1}", ip, _LastSync);
					
					_Playing = true;
					
					_Thread = new Thread(new ThreadStart(()=>
					{
						bool playing;
						lock(_Sync)
						{
							playing = _Playing;
						}
						
						while (playing)
						{
							Thread.Sleep(5000);
							
							SetQuality(_IP, UI.QUALITY);
							
							RestartIfNeeded();
							
							SyncAudio(false);
							
							lock(_Sync)
							{
								playing = _Playing;
							}
						}
					}));
					
					_Thread.Start();
				}
				return true;
			}
			
			static bool IsRemoteAlive()
			{
				const int MAX_RETRY = 3;
				
				while (_AudioProc.Preparing || _VideoProc.Preparing)
				{
					//Log.WriteLine("audio:{0} video:{1}", _AudioProc.State, _VideoProc.State);
					
					Thread.Sleep (1000);
				}
				
				if (_AudioProc.Alive && _VideoProc.Alive)
					return true;
				
				//Log.WriteLine("IsRemoteAlive() = false {0}, {1}", _AudioProc.State, _VideoProc.State);
				
				return false;
			}
			
			static bool RestartIfNeeded()
			{
				if (!IsRemoteAlive())
				{
					UI.Focus();
					
					// wait for 10 sec and then auto restart
					for(int i = 10; i > 0; --i)
					{
						UI.SetStatus (string.Format("Interrupted, reconnect in {0}", i));
						Thread.Sleep (1000);
					}
					
					UI.SetStatus ("Connecting");
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
			
			static public void SyncAudio(bool forceSync)
			{
				lock(_Sync)
				{
					if (!_AudioProc.Alive || !_VideoProc.Alive)
						return;
					
					var time = DateTime.Now.Subtract (_LastSync);
					if (time.Minutes >= SYNC_INTERVAL || forceSync)
					{
						_AudioProc.Stop();
						_AudioProc.Play();
						
						_LastSync = DateTime.Now;
						Log.WriteLine("SyncAudio at {0}", _LastSync);
					}
				}
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
				const int MAX_RETRY = 5;
				
				for (int i=0; i <MAX_RETRY; ++i)
				{
					try
					{
						string url = string.Format ("http://{0}:8080/settings/quality?set={1}", ip, value);
						var request = WebRequest.Create(url) as HttpWebRequest;
						request.Timeout = 5000;
						
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
						Thread.Sleep (500);
					}
				}
				
				Log.WriteLine("SetQuality failed all {0} passes", MAX_RETRY);
				
				return false;
			}
			
			static void StartAudioAndVideoPlayback(string ip, bool fullscreen)
			{
				Log.WriteLine (NETWORK_CACHING_OPTION);
				_AudioProc = _vlc.CreatePlayback(
					string.Format ("http://{0}:8080/audio.wav", ip), 
					new string[] {
						//"--no-video",
						FILE_CACHING_OPTION,
						LIVE_CACHING_OPTION,
						DISK_CACHING_OPTION,
						NETWORK_CACHING_OPTION,
					});
				_AudioProc.Play();
				
				_VideoProc = _vlc.CreatePlayback(
					string.Format ("http://{0}:8080/video4flash", ip), 
					new string[] {
						//"--no-audio",
						FILE_CACHING_OPTION,
						LIVE_CACHING_OPTION,
						DISK_CACHING_OPTION,
						NETWORK_CACHING_OPTION,
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
			
			static public int QUALITY = 38;
			
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
				FULLSCREEN = !DevMode;
				
				var frm = new Form() {
					Text = "TobChurch Broadcast",
					Size = new Size (480, 240),
					Padding = new Padding (2),
					Font = new Font ("Consolas", 12.0f),
					FormBorderStyle = FormBorderStyle.FixedToolWindow,
					MaximizeBox = false,
				};
				
				frm.SuspendLayout();
				
				{
					GroupBox gb = new GroupBox() {
						Dock = DockStyle.Top,
						Height = 80,
						Text = "Broadcast",
						Padding = new Padding (8),
					};
					frm.Controls.Add (gb);
					_gb = gb;
					if(DevMode)
					{
						Button bt = new Button() {
							Dock = DockStyle.Left,
							Text = "SYNC",
							Width = 100,
						};
						bt.Click += (s, e) => {
							Streaming.SyncAudio(true);
						};
						gb.Controls.Add (bt);
					}
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
							
							UI.SetStatus ("Connecting");
					
							if (!Streaming.Start(IP, FULLSCREEN))
							{
								Warn(string.Format("Cannot connect to {0}!", IP));
								UI.SetStatus ("");
							}
						};
						gb.Controls.Add (bt);
					}
				}
				
				{
					GroupBox gb = new GroupBox() {
						Dock = DockStyle.Top,
						Height = 130,
						Text = "Options",
					};
					frm.Controls.Add (gb);
					
					#region QUALITY
					{
						Panel p = new Panel() {
							Dock = DockStyle.Top,
							Height = 35,
						};
						gb.Controls.Add (p);
						{
							NumericUpDown  ud = new NumericUpDown() {
								Dock = DockStyle.Left,
								Maximum = 100,
								Minimum = 1,
								Value = QUALITY,
								Width = 60,
							};
							p.Controls.Add (ud);
							ud.ValueChanged += (s, e) =>
							{
								QUALITY = (int)ud.Value;
							};
						}
						{
							Label la = new Label() {
								Text = "Quality ",
								Dock = DockStyle.Left,
								TextAlign = ContentAlignment.MiddleLeft,
								AutoSize = true,
							};
							p.Controls.Add (la);
						}
					}
					#endregion
					
					#region IP
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
					#endregion
					
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
		
		class Log
		{
			static public void WriteLine(object a)
			{
				if (DevMode)
					Console.WriteLine(a);
			}
			
			static public void WriteLine(string fmt, object a)
			{
				if (DevMode)
					Console.WriteLine(fmt, a);
			}
			
			static public void WriteLine(string fmt, object a, object b)
			{
				if (DevMode)
					Console.WriteLine(fmt, a, b);
			}
			
			static public void WriteLine(string fmt, object a, object b, object c)
			{
				if (DevMode)
					Console.WriteLine(fmt, a, b, c);
			}
			
			static public void WriteLine(string fmt, object a, object b, object c, object d)
			{
				if (DevMode)
					Console.WriteLine(fmt, a, b, c, d);
			}
		}
		
		static public void Main (string[] args)
		{
			foreach (string a in args)
			{
				if (a.ToLower() == "dev")
					DevMode = true;
			}
			
			Log.WriteLine ("DevMode = {0}", DevMode);
			
			Streaming.Init (args);
			
			UI.Init (args);
			
			Streaming.Quit();
		}
	}	// class App

}	// namespace TOB
