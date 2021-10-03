using System;
using System.Globalization;
using System.IO;
using AppKit;
using Foundation;

namespace Testy
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        public AppDelegate()
        {
        }

        private void Log (string s)
        {
            using (var fs = new FileStream("/Users/mic/Downloads/picasa.log", FileMode.Append, FileAccess.Write))
            using (var writer = new StreamWriter(fs))
            {
                writer.WriteLine($"{DateTime.Now.ToString(CultureInfo.InvariantCulture)} {s}");
                writer.Flush();
            }
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            Log("DidFinishLaunching");
            // Insert code here to initialize your application
        }

        public override void WillTerminate(NSNotification notification)
        {
            Log("WillTerminate");
            // Insert code here to tear down your application
        }

        [Export("application:openFile:")]
        public override bool OpenFile(NSApplication sender, string filename)
        {
            Log($"Open file '{filename}'");
            return true;
        }
    }
}
