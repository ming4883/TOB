//css_import DyLib
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TOB
{
    public class VLC : IDisposable
    {
		private IntPtr mInst;
		
        public VLC (string basedir)
        {
            IntPtr module = DyLib.Get("libvlc", basedir);

            if (IntPtr.Zero == module)
                throw new Exception(string.Format ("GetModule failed: {0}", basedir));

            InitBindings (module);
		
			Console.WriteLine("using VLC version: {0}", libvlc_get_version());
			mInst = libvlc_new(0, null);
        }
		
        public void Dispose()
        {
			libvlc_release (mInst);
        }
		
		public class MediaPlayback : IDisposable
		{
			private VLC mVLC;
			private IntPtr mMedia;
			private IntPtr mPlayer;
			
			public MediaPlayback(VLC vlc, string mrl, string[] options)
			{
				mVLC = vlc;
				
				mMedia = vlc.libvlc_media_new_location (vlc.mInst, mrl);
				
				if (null != options)
				{
					foreach (var opt in options)
						vlc.libvlc_media_add_option (mMedia, opt);
				}
				
				mPlayer = vlc.libvlc_media_player_new_from_media (mMedia);
			}
			
			public void Dispose()
			{
				mVLC.libvlc_media_player_release(mPlayer);
				mVLC.libvlc_media_release(mMedia);
				mVLC = null;
			}
			
			public libvlc_state_t State
			{
				get { return mVLC.libvlc_media_get_state (mMedia); }
			}
			
			public bool Alive
			{
				get
				{
					libvlc_state_t state = State;
					return state != libvlc_state_t.libvlc_Ended 
						&& state != libvlc_state_t.libvlc_Error;
				}
			}
			
			public bool Preparing
			{
				get
				{
					libvlc_state_t state = State;
					return state == libvlc_state_t.libvlc_Opening 
						|| state == libvlc_state_t.libvlc_Buffering;
				}
			}
			
			public void SetHWND(IntPtr hwnd)
			{
				mVLC.libvlc_media_player_set_hwnd (mPlayer, hwnd);
			}
			
			public void Play()
			{
				mVLC.libvlc_media_player_play (mPlayer);
			}
			
			public void Stop()
			{
				mVLC.libvlc_media_player_stop (mPlayer);
			}
			
			public void SetPause(bool doPause)
			{
				mVLC.libvlc_media_player_set_pause (mPlayer, doPause ? 1 : 0);
			}
			
			public void SetVolume(int volume)
			{
				mVLC.libvlc_audio_set_volume (mPlayer, volume);
			}
			
			public void SetAudioDelay(Int64 delay)
			{
				mVLC.libvlc_audio_set_delay (mPlayer, delay);
			}
			
			public bool Fullscreen
			{
				set
				{
					mVLC.libvlc_set_fullscreen (mPlayer, value ? 1 : 0);
				}
				
				get
				{
					return mVLC.libvlc_get_fullscreen (mPlayer) > 0;
				}
			}
		}
		
		public MediaPlayback CreatePlayback(string mrl, string[] options)
		{
			return new MediaPlayback(this, mrl, options);
		}
		
		#region Binding
		private void InitBindings(IntPtr module)
		{
			libvlc_get_version = DyLib.GetProc<libvlc_get_version_delegate>(module);
			libvlc_new = DyLib.GetProc<libvlc_new_delegate>(module);
			libvlc_release = DyLib.GetProc<libvlc_release_delegate>(module);
			
			libvlc_media_new_location = DyLib.GetProc<libvlc_media_new_location_delegate>(module);
			libvlc_media_release = DyLib.GetProc<libvlc_media_release_delegate>(module);
			libvlc_media_get_state = DyLib.GetProc<libvlc_media_get_state_delegate>(module);
			libvlc_media_add_option = DyLib.GetProc<libvlc_media_add_option_delegate>(module);
			
			libvlc_media_player_new = DyLib.GetProc<libvlc_media_player_new_delegate>(module);
			libvlc_media_player_new_from_media = DyLib.GetProc<libvlc_media_player_new_from_media_delegate>(module);
			libvlc_media_player_release = DyLib.GetProc<libvlc_media_player_release_delegate>(module);
			libvlc_media_player_set_hwnd = DyLib.GetProc<libvlc_media_player_set_hwnd_delegate>(module);
			libvlc_media_player_play = DyLib.GetProc<libvlc_media_player_play_delegate>(module);
			libvlc_media_player_stop = DyLib.GetProc<libvlc_media_player_stop_delegate>(module);
			libvlc_media_player_set_pause = DyLib.GetProc<libvlc_media_player_set_pause_delegate>(module);
			libvlc_audio_set_volume = DyLib.GetProc<libvlc_audio_set_volume_delegate>(module);
			libvlc_audio_set_delay = DyLib.GetProc<libvlc_audio_set_delay_delegate>(module);
			libvlc_set_fullscreen = DyLib.GetProc<libvlc_set_fullscreen_delegate>(module);
			libvlc_get_fullscreen = DyLib.GetProc<libvlc_get_fullscreen_delegate>(module);
			
			/*
			libvlc_media_discoverer_new = DyLib.GetProc<libvlc_media_discoverer_new_delegate>(module);
			libvlc_media_discoverer_new_from_name = DyLib.GetProc<libvlc_media_discoverer_new_from_name_delegate>(module);
			libvlc_media_discoverer_release = DyLib.GetProc<libvlc_media_discoverer_release_delegate>(module);
			libvlc_media_discoverer_start = DyLib.GetProc<libvlc_media_discoverer_start_delegate>(module);
			libvlc_media_discoverer_stop = DyLib.GetProc<libvlc_media_discoverer_stop_delegate>(module);
			libvlc_media_discoverer_is_running = DyLib.GetProc<libvlc_media_discoverer_is_running_delegate>(module);
			*/
		}

		#region Core
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate string libvlc_get_version_delegate();
        public libvlc_get_version_delegate libvlc_get_version;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libvlc_new_delegate(int argc, [MarshalAs(UnmanagedType.LPStr)]string argv);
        public libvlc_new_delegate libvlc_new;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_release_delegate(IntPtr instance);
        public libvlc_release_delegate libvlc_release;
		
		#endregion
		
		#region Media
		[Flags]
        public enum libvlc_state_t
        {
			libvlc_NothingSpecial,
			libvlc_Opening,
			libvlc_Buffering,
			libvlc_Playing,
			libvlc_Paused,
			libvlc_Stopped,
			libvlc_Ended,
			libvlc_Error,
        }
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libvlc_media_new_location_delegate(IntPtr instance, [MarshalAs(UnmanagedType.LPStr)]string mrl);
        public libvlc_media_new_location_delegate libvlc_media_new_location;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_media_release_delegate(IntPtr media);
        public libvlc_media_release_delegate libvlc_media_release;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate libvlc_state_t libvlc_media_get_state_delegate(IntPtr media);
        public libvlc_media_get_state_delegate libvlc_media_get_state;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_media_add_option_delegate(IntPtr media, [MarshalAs(UnmanagedType.LPStr)]string option);
        public libvlc_media_add_option_delegate libvlc_media_add_option;
		
		#endregion
		
		#region MediaPlayer
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libvlc_media_player_new_delegate(IntPtr instance);
        public libvlc_media_player_new_delegate libvlc_media_player_new;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libvlc_media_player_new_from_media_delegate(IntPtr media);
        public libvlc_media_player_new_from_media_delegate libvlc_media_player_new_from_media;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_media_player_release_delegate(IntPtr mediaplayer);
        public libvlc_media_player_release_delegate libvlc_media_player_release;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libvlc_media_player_set_hwnd_delegate(IntPtr mediaplayer, IntPtr hwnd);
        public libvlc_media_player_set_hwnd_delegate libvlc_media_player_set_hwnd;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_media_player_play_delegate(IntPtr mediaplayer);
        public libvlc_media_player_play_delegate libvlc_media_player_play;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_media_player_stop_delegate(IntPtr mediaplayer);
        public libvlc_media_player_stop_delegate libvlc_media_player_stop;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_media_player_set_pause_delegate(IntPtr mediaplayer, int do_pause);
        public libvlc_media_player_set_pause_delegate libvlc_media_player_set_pause;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_audio_set_volume_delegate(IntPtr mediaplayer, int volume);
        public libvlc_audio_set_volume_delegate libvlc_audio_set_volume;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_audio_set_delay_delegate(IntPtr mediaplayer, Int64 delay);
        public libvlc_audio_set_delay_delegate libvlc_audio_set_delay;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_set_fullscreen_delegate(IntPtr mediaplayer, int fullscreen);
        public libvlc_set_fullscreen_delegate libvlc_set_fullscreen;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int libvlc_get_fullscreen_delegate(IntPtr mediaplayer);
        public libvlc_get_fullscreen_delegate libvlc_get_fullscreen;
		
		#endregion
		
		#region MediaDiscovery
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libvlc_media_discoverer_new_delegate(IntPtr instance, [MarshalAs(UnmanagedType.LPStr)]string name);
        public libvlc_media_discoverer_new_delegate libvlc_media_discoverer_new;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libvlc_media_discoverer_new_from_name_delegate(IntPtr instance, [MarshalAs(UnmanagedType.LPStr)]string name);
        public libvlc_media_discoverer_new_from_name_delegate libvlc_media_discoverer_new_from_name;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_media_discoverer_release_delegate(IntPtr discoverer);
        public libvlc_media_discoverer_release_delegate libvlc_media_discoverer_release;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_media_discoverer_start_delegate(IntPtr discoverer);
        public libvlc_media_discoverer_start_delegate libvlc_media_discoverer_start;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libvlc_media_discoverer_stop_delegate(IntPtr discoverer);
        public libvlc_media_discoverer_stop_delegate libvlc_media_discoverer_stop;
		
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int libvlc_media_discoverer_is_running_delegate(IntPtr discoverer);
        public libvlc_media_discoverer_is_running_delegate libvlc_media_discoverer_is_running;
		
		#endregion
    
		#endregion
	}
}
