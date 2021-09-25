using System;
using System.IO;
using System.Runtime.InteropServices;
using AppKit;
using Foundation;
using NLog;

namespace Picasa.OSX
{
    [Register("AppDelegate2")]
    public class AppDelegate2 : NSApplicationDelegate
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void InitializeToolkit()
        {
            // First, dlopen libxammac.dylib from the same dir we loaded the Xamarin.Mac dll
            var dylibPath = Path.Combine(Path.GetDirectoryName(typeof(AppDelegate2).Assembly.Location), "libxammac.dylib");
            if (dlopen(dylibPath, 0) == IntPtr.Zero)
            {
                var errPtr = dlerror();
                var errStr = (errPtr == IntPtr.Zero) ? "<unknown error>" : Marshal.PtrToStringAnsi(errPtr);
                Console.WriteLine("WARNING: Cannot load {0}: {1}", dylibPath, errStr);
            }
        }

        public override NSApplicationTerminateReply ApplicationShouldTerminate(NSApplication sender)
        {
            Logger.Info("ApplicationShouldTerminate");
            return NSApplicationTerminateReply.Now;
        }

        public override void WillTerminate(NSNotification notification)
        {
            Logger.Info("WillTerminate");
        }
        
        public override bool OpenFile(NSApplication sender, string filename)
        {
            Logger.Info($"Open file '{filename}'");
            return true;
        }

        public override void OpenFiles(NSApplication sender, string[] filenames)
        {
            Logger.Info($"Open files '{filenames.Length}'");
        }

        public override bool OpenTempFile(NSApplication sender, string filename)
        {
            Logger.Info($"OpenTempFile '{filename}'");
            return true;
        }

        public override bool ApplicationShouldOpenUntitledFile(NSApplication sender)
        {
            Logger.Info($"ApplicationShouldOpenUntitledFile");
            return true;
        }

        public override bool OpenFileWithoutUI(NSObject sender, string filename)
        {
            Logger.Info($"OpenFileWithoutUI '{filename}'");
            return true;
        }

        public override bool ApplicationOpenUntitledFile(NSApplication sender)
        {
            Logger.Info($"ApplicationOpenUntitledFile");
            return true;
        }

        /// <summary>
        /// Because we are creating our own mac application delegate we are removing / overriding
        /// the one that Avalonia creates. This causes the application to not be handled as it should.
        /// This is the Avalonia Implementation: https://github.com/AvaloniaUI/Avalonia/blob/5a2ef35dacbce0438b66d9f012e5f629045beb3d/native/Avalonia.Native/src/OSX/app.mm
        /// So what we are doing here is re-creating this implementation to mimick their behavior.
        /// </summary>
        /// <param name="notification"></param>
        public override void WillFinishLaunching(NSNotification notification)
        {
            Logger.Info("WillFinishLaunching");

            if (NSApplication.SharedApplication.ActivationPolicy != NSApplicationActivationPolicy.Regular)
            {
                foreach (var x in NSRunningApplication.GetRunningApplications(@"com.apple.dock"))
                {
                    x.Activate(NSApplicationActivationOptions.ActivateIgnoringOtherWindows);
                    break;
                }

                NSApplication.SharedApplication.ActivationPolicy = NSApplicationActivationPolicy.Regular;
            }
        }

        /// <summary>
        /// Because we are creating our own mac application delegate we are removing / overriding
        /// the one that Avalonia creates. This causes the application to not be handled as it should.
        /// This is the Avalonia Implementation: https://github.com/AvaloniaUI/Avalonia/blob/5a2ef35dacbce0438b66d9f012e5f629045beb3d/native/Avalonia.Native/src/OSX/app.mm
        /// So what we are doing here is re-creating this implementation to mimick their behavior.
        /// </summary>
        /// <param name="notification"></param>
        public override void DidFinishLaunching(NSNotification notification)
        {
            Logger.Info("DidFinishLaunching");

            NSRunningApplication.CurrentApplication.Activate(
                NSApplicationActivationOptions.ActivateIgnoringOtherWindows);
        }

        [DllImport("/usr/lib/libSystem.dylib")]
        static extern IntPtr dlopen(string path, int mode);

        [DllImport("/usr/lib/libSystem.dylib")]
        static extern IntPtr dlerror();
    }
}