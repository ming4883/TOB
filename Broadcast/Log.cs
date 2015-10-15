//css_import Settings
using System;

namespace TOB
{
	class Log
	{
		static public void WriteLine(object a)
		{
			if (Settings.DevMode)
				Console.WriteLine(a);
		}
		
		static public void WriteLine(string fmt, object a)
		{
			if (Settings.DevMode)
				Console.WriteLine(fmt, a);
		}
		
		static public void WriteLine(string fmt, object a, object b)
		{
			if (Settings.DevMode)
				Console.WriteLine(fmt, a, b);
		}
		
		static public void WriteLine(string fmt, object a, object b, object c)
		{
			if (Settings.DevMode)
				Console.WriteLine(fmt, a, b, c);
		}
		
		static public void WriteLine(string fmt, object a, object b, object c, object d)
		{
			if (Settings.DevMode)
				Console.WriteLine(fmt, a, b, c, d);
		}
	}
}	// namespace TOB
