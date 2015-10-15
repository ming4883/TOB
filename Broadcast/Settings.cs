using System;

namespace TOB
{
	
	class Settings
	{
		static public bool DevMode = false;
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
		
	}
	

}	// namespace TOB
