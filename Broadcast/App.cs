////css_co /unsafe;
//css_import Package
//css_import VLC
//css_import UI
//css_import Settings
//css_import Log
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;

using System.Net;
using System.Xml;

namespace TOB
{
	class App
	{
		class Streaming
		{
			// https://wiki.videolan.org/VLC_command-line_help
			
			const string FILE_CACHING_OPTION = "--file-caching=" + Settings.REMOTE_CACHING;
			const string LIVE_CACHING_OPTION = "--live-caching=" + Settings.REMOTE_CACHING;
			const string DISK_CACHING_OPTION = "--disk-caching=" + Settings.REMOTE_CACHING;
			const string NETWORK_CACHING_OPTION = "--network-caching=" + Settings.REMOTE_CACHING;
			
			const int SYNC_INTERVAL = 15;
			const int SYNC_INTERVAL_ONCE = 3;
			
			const string ADB_PATH = "adb/adb.exe";
			
			static Object _Sync = new Object();
			static bool _Playing = false;
			
			static VLC _vlc = null;
			static VLC.MediaPlayback _VideoProc = null;
			static VLC.MediaPlayback _AudioProc = null;
			
			static Thread _SyncThread = null;
			static DateTime _LastSync;
			static string _LastIP = "127.0.0.1";
			
			static bool _FirstReset = true;
			static UI.PlaybackForm _PlaybackForm = null;
			
			
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
			
			static bool AdbKill(string adbPath)
			{
				int ret = -1;
				
				try
				{
					Process p = new Process();
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.FileName = adbPath;
					p.StartInfo.Arguments = "kill-server";
					p.Start();
					p.WaitForExit(-1);
					
					ret = p.ExitCode;
				}
				catch (Exception e)
				{
					Console.WriteLine (e);
					return false;
				}
				
				return ret == 0;
			}
			
			static bool AdbPortForward(string adbPath, int port)
			{
				int ret = -1;
				
				try
				{
					Process p = new Process();
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.FileName = adbPath;
					p.StartInfo.Arguments = string.Format("forward tcp:{0} tcp:{0}", port);
					p.Start();
					p.WaitForExit(-1);
					
					ret = p.ExitCode;
				}
				catch (Exception e)
				{
					Console.WriteLine (e);
					return false;
				}
				
				return ret == 0;
			}
			
			static public bool Start()
			{
				_LastIP = "127.0.0.1";
					
				AdbKill(ADB_PATH);
				
				if (!AdbPortForward(ADB_PATH, 8080))
				{
					_LastIP = Settings.IP;
					Log.WriteLine ("adb port forward failed, use remote ip: " + _LastIP);
				}
				
				lock (_Sync)
				{
					_PlaybackForm = UI.ShowPlaybackForm (Settings.FULLSCREEN);
					
					StartAudioAndVideoPlayback (_LastIP);
					
					if (!IsRemoteAlive())
					{
						Stop();
						return false;
					}
						
					SetFocus(_LastIP);
					
					_FirstReset = true;
					_Playing = true;
					
					_SyncThread = new Thread(new ThreadStart(()=>
					{
						bool playing;
						lock(_Sync) { playing = _Playing; }
						
						while (playing)
						{
							Thread.Sleep(5000);
							
							SetQuality(_LastIP, Settings.QUALITY);
							
							RestartIfNeeded();
							
							SyncAudio(false);
							
							lock(_Sync) { playing = _Playing; }
						}
					}));
					
					
					_LastSync = DateTime.Now;
					
					Log.WriteLine("Streaming from {0} at {1}", _LastIP, _LastSync);
					
					
					_SyncThread.Start();
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
					
					AdbKill(ADB_PATH);
					
					// wait for 10 sec and then auto restart
					for(int i = 10; i > 0; --i)
					{
						UI.SetStatus (string.Format("Interrupted, reconnect in {0}", i));
						Thread.Sleep (1000);
					}
					
					if (!AdbPortForward(ADB_PATH, 8080))
					{
						Log.WriteLine ("adb port forward failed");
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
					{
						//Log.WriteLine("SyncAudio video / audio not alive");
						return;
					}
					
					int interval = SYNC_INTERVAL;
					if (_FirstReset)
					{
						interval = SYNC_INTERVAL_ONCE;
					}
					
					var time = DateTime.Now.Subtract (_LastSync);
					if (time.Minutes >= interval || forceSync)
					{	
						if (null != _AudioProc)
						{
							_AudioProc.Stop();
							_AudioProc.Dispose();
						}
						
						if (null != _VideoProc)
						{
							_VideoProc.Stop();
							_VideoProc.Dispose();
						}
						
						StartAudioAndVideoPlayback (_LastIP);
						
						_LastSync = DateTime.Now;
						_FirstReset = false;
						Log.WriteLine("SyncAudio at {0}", _LastSync);
					}
					else
					{
						//Log.WriteLine("SyncAudio not yet {0} / {1}", time, interval);
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
					if (null != _SyncThread)
					{
						_SyncThread.Abort();
						_SyncThread = null;
						
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
					
					if (null != _PlaybackForm)
					{
						_PlaybackForm.Close();
						_PlaybackForm.Dispose();
						_PlaybackForm = null;
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
			
			static bool SetFocus(string ip)
			{
				const int MAX_RETRY = 5;
				
				for (int i=0; i <MAX_RETRY; ++i)
				{
					try
					{
						string url = string.Format ("http://{0}:8080/focus", ip);
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
				
				Log.WriteLine("SetFocus failed all {0} passes", MAX_RETRY);
				
				return false;
			}
			
			static void StartAudioAndVideoPlayback(string ip)
			{
				if (Settings.AUDIO_SOURCE == Settings.AUDIO_SOURCE_NETWORK)
				{
					_AudioProc = _vlc.CreatePlayback(
						string.Format ("http://{0}:8080/audio.wav", ip), 
						new string[] {
							//"--no-video",
							FILE_CACHING_OPTION,
							LIVE_CACHING_OPTION,
							DISK_CACHING_OPTION,
							NETWORK_CACHING_OPTION,
						});
					Log.WriteLine("Audio from network");
				}
				else if (Settings.AUDIO_SOURCE == Settings.AUDIO_SOURCE_MIC_IN)
				{
					_AudioProc = _vlc.CreatePlayback(
						"dshow://", 
						new string[] {
							":dshow-vdev=none",
							":live-caching=" + Settings.CAPTURE_CACHING,
						});
					//_AudioProc.SetAudioDelay (-1000);
					Log.WriteLine("Audio from mic-in");
				}
				
				_VideoProc = _vlc.CreatePlayback(
					string.Format ("http://{0}:8080/video", ip), 
					new string[] {
						//"--no-audio",
						FILE_CACHING_OPTION,
						LIVE_CACHING_OPTION,
						DISK_CACHING_OPTION,
						NETWORK_CACHING_OPTION,
					});
					
				_VideoProc.SetHWND (_PlaybackForm.HWND);
				_VideoProc.Play();
				
				if (null != _AudioProc)
				{
					_AudioProc.SetVolume (100);
					_AudioProc.Play();
				}
			}
		}
		
		static public void Main (string[] args)
		{
			foreach (string a in args)
			{
				if (a.ToLower() == "dev")
					Settings.DevMode = true;
			}
			
			Log.WriteLine ("DevMode = {0}", Settings.DevMode);
			
			Streaming.Init (args);
			
			UI.OnStreamingStart = () =>
			{
				Streaming.Stop();
						
				UI.SetStatus ("Connecting");
		
				if (!Streaming.Start())
				{
					UI.Warn(string.Format("Cannot connect to {0}!", Settings.IP));
					UI.SetStatus ("");
				}
			};
			
			UI.OnStreamingSync = () =>
			{
				Streaming.SyncAudio(true);
			};
			
			UI.OnStreamingStop = () =>
			{
				Streaming.Stop();
				UI.SetStatus("");
			};
			
			UI.Init();
			
			Streaming.Quit();
		}
	}	// class App

}	// namespace TOB
