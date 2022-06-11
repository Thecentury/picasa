using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using AppKit;
using Foundation;

namespace Picasa
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        private static void Log(string s)
        {
            var personal = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var fullLogPath = Path.Combine(personal, "Downloads/picasa.log");
            using (var fs = new FileStream(fullLogPath, FileMode.Append, FileAccess.Write))
            using (var writer = new StreamWriter(fs))
            {
                writer.WriteLine($"{DateTime.Now.ToString(CultureInfo.InvariantCulture)} {s}");
                writer.Flush();
            }
        }
        
        // ReSharper disable once UnusedParameter.Local
        private static void LogVerbose(string _) { }

        public override void DidFinishLaunching(NSNotification notification)
        {
            LogVerbose("DidFinishLaunching");
        }

        public override void WillTerminate(NSNotification notification)
        {
            LogVerbose("WillTerminate");
        }

        [Export("application:openFile:")]
        public override bool OpenFile(NSApplication sender, string filename)
        {
            LogVerbose($"Open file '{filename}'");
            try
            {
                using (var currentProcess = Process.GetCurrentProcess())
                {
                    var personal = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    var picasaLocation = Path.Combine(personal, ".picasa", "Picasa.dll");
                    var psi = new ProcessStartInfo
                    {
                        FileName = "/usr/local/share/dotnet/dotnet",
                        Arguments = $"{picasaLocation} \"{filename}\" {currentProcess.Id}",
                        WindowStyle = ProcessWindowStyle.Maximized
                    };

                    LogVerbose($"Launching Picasa.dll. Location: '{picasaLocation}', PID: {currentProcess.Id}");
                    
                    using (var _ = Process.Start(psi))
                    {
                    }
                }

                return true;
            }
            catch (Exception exc)
            {
                Log($"Unhandled exception when opening file '{filename}': {exc}");
                return false;
            }
        }
    }
}
