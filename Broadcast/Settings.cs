using System;

namespace TOB
{
	
	class Settings
	{
		public static bool DevMode = false;
		/*
		public static int IP1 = 127;
		public static int IP2 = 0;
		public static int IP3 = 0;
		public static int IP4 = 1;
		*/
		public static int IP1 = 192;
		public static int IP2 = 168;
		public static int IP3 = 1;
		public static int IP4 = 112;
		
		public static string IP
		{
			get
			{
				return string.Format ("{0}.{1}.{2}.{3}", IP1, IP2, IP3, IP4);
			}
		}
		
		public static int QUALITY = 67;
		
		public static bool FULLSCREEN = true;
		
		public const int AUDIO_SOURCE_NETWORK = 0;
		public const int AUDIO_SOURCE_MIC_IN = 1;
		public static int AUDIO_SOURCE = AUDIO_SOURCE_MIC_IN;
		
		public const string REMOTE_CACHING = "500";
		public const string CAPTURE_CACHING = "1000";
	}
	

}	// namespace TOB
