//css_reference "System.IO.Compression.FileSystem.dll"
using System;
using System.IO;
using System.IO.Compression;

namespace TOB
{
	class Package
	{
		public static string Extract (string zipPath, string dstPath)
		{
			if (Directory.Exists (dstPath))
				return "Directory already exists";
			
			if (!File.Exists (zipPath))
				return "Zip file not exists";
			
			try
			{
				Console.WriteLine ("Extracting {0}...", zipPath);
				ZipFile.ExtractToDirectory (zipPath, dstPath);
			}
			catch (Exception e)
			{
				return e.ToString();
			}
			
			return null;
		}
		
		public static string ExtractIf (string zipPath, string dstPath, Predicate<string> shouldExtract)
		{
			if (null != shouldExtract && false == shouldExtract (dstPath))
			{
				return "shouldExtract() return false";
			}
			
			return Extract (zipPath, dstPath);
		}
	}
}
