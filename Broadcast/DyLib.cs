using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TOB
{
    public static class DyLib
    {
        internal interface ILoader
        {
            IntPtr Open(string moduleName, string basePath);

            IntPtr Lookup(IntPtr module, String method);
        }

        internal class LoaderWindows : ILoader
        {
            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport("kernel32")]
            static extern IntPtr GetProcAddress(IntPtr hModule, String procname);

            public IntPtr Open(string moduleName, string basePath)
            {
                string assemblyDirectory;

                if (Path.IsPathRooted(basePath))
                    assemblyDirectory = basePath;
                else
                    assemblyDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), basePath));

                assemblyDirectory = Path.Combine(assemblyDirectory, Environment.Is64BitProcess ? "x64" : "x86");

				
				string lastCWD = Directory.GetCurrentDirectory();
				Directory.SetCurrentDirectory(assemblyDirectory);
				
                string modulePath = Path.Combine(assemblyDirectory, moduleName + ".dll");

				Debug.Print(string.Format("Loading {0}", modulePath));

                IntPtr ret = LoadLibrary(modulePath);
				Directory.SetCurrentDirectory(lastCWD);
				return ret;
            }

            public IntPtr Lookup(IntPtr module, string method)
            {
                return GetProcAddress(module, method);
            }
        }

        internal class LoaderPosix : ILoader
        {
            [DllImport("libdl")]
            static extern IntPtr dlopen(string fileName, int flags);

            [DllImport("libdl")]
            static extern IntPtr dlerror();

            [DllImport("libdl")]
            static extern IntPtr dlsym(IntPtr handle, String symbol);

            public IntPtr Open(string moduleName, string basePath)
            {
                string assemblyDirectory;

                if (Path.IsPathRooted(basePath))
                    assemblyDirectory = basePath;
                else
                    assemblyDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), basePath));

                assemblyDirectory = Path.Combine(assemblyDirectory, Environment.Is64BitProcess ? "x64" : "x86");

                const int RTLD_NOW = 2;
                string modulePath = Path.Combine(assemblyDirectory, "lib" + moduleName + ".dylib");

                Debug.Print(string.Format("Loading {0}", modulePath));

                IntPtr ret = dlopen(modulePath, RTLD_NOW);

                if (IntPtr.Zero == ret)
                {
                    Debug.Print(string.Format("{0}", Marshal.PtrToStringAnsi(dlerror())));
                }

                return ret;
            }

            public IntPtr Lookup(IntPtr module, string method)
            {
                return dlsym(module, method);
            }
        }

        public static bool IsRunningOnMono()
        {
            // Detect the Mono runtime (code taken from http://mono.wikia.com/wiki/Detecting_if_program_is_running_in_Mono).
            Type t = Type.GetType("Mono.Runtime");
            return t != null;
        }

        public static bool IsRunningOnWindows()
        {
            return
                System.Environment.OSVersion.Platform == PlatformID.Win32NT ||
                System.Environment.OSVersion.Platform == PlatformID.Win32S ||
                System.Environment.OSVersion.Platform == PlatformID.Win32Windows ||
                System.Environment.OSVersion.Platform == PlatformID.WinCE;
        }

        internal static ILoader m_Loader;

        internal static ILoader Loader
        {
            get
            {
                if (null == m_Loader)
                {
                    if (IsRunningOnWindows())
                        m_Loader = new LoaderWindows();
                    else
                        m_Loader = new LoaderPosix();
                }
                return m_Loader;
            }
        }

        internal static IntPtr Get(string moduleName, string basePath)
        {
            return Loader.Open(moduleName, basePath);
        }

        internal static T GetProc<T>(IntPtr module) where T : class
        {
            return GetProc<T>(module, typeof(T).Name.Replace("_delegate", ""));
        }

        internal static T GetProc<T>(IntPtr module, string name) where T : class
        {
            //Console.WriteLine(name);
            IntPtr fptr = Loader.Lookup(module, name);
            if (fptr == IntPtr.Zero)
            {
                Console.WriteLine(string.Format("Proc {0} not found!", name));
                return null;
            }
            return Marshal.GetDelegateForFunctionPointer(fptr, typeof(T)) as T;
        }
    }

}