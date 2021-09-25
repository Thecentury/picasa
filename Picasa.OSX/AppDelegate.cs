﻿using System;
using System.Runtime.InteropServices;
using MonoMac.AppKit;
using MonoMac.Foundation;
using NLog;

namespace Picasa.OSX
{
    /// <summary>
    /// This class is an AppDelegate helper specifically for Mac OSX
    /// Int it's infinite wisdom and unlike Linux and or Windows Mac does not pass in the URL from a sqrl:// invokation
    /// directly as a startup app paramter, instead it uses a System Event to do this which has to be registered
    /// and listed to.
    /// This requires us to use MonoMac to make it work with .net core
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public bool IsFinishedLaunching = false;

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern int IOServiceGetMatchingServices(uint masterPort, IntPtr matching, ref int existing);

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern uint IOServiceGetMatchingService(uint masterPort, IntPtr matching);

#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        static extern IntPtr IOServiceMatching(string s);

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern IntPtr IORegistryEntryCreateCFProperty(uint entry, IntPtr key, IntPtr allocator, uint options);

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern int IOObjectRelease(int o);

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern int IOIteratorNext(int o);

        [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
        static extern int IORegistryEntryCreateCFProperties(int entry, out IntPtr eproperties, IntPtr allocator,
            uint options);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        static extern bool CFNumberGetValue(IntPtr number, int theType, out long value);

        // // Instance Window of our App
        // Window mainWindow = null;
        //
        //
        //
        // public AppDelegate(Window mainWindow)
        // {
        //     this.mainWindow = mainWindow;
        //     Init();
        // }
        public AppDelegate()
        {
            Init();
        }

        /// <summary>
        /// Registers an event for handling URL Invokation
        /// </summary>
        private void Init()
        {
            // Log.Information("Initializing Mac App Delegate");
            // //Register this Apple Delegate globablly with Avalonia for Later Use
            // AvaloniaLocator.CurrentMutable.Bind<AppDelegate>().ToConstant(this);
            NSAppleEventManager.SharedAppleEventManager.SetEventHandler(this,
                new MonoMac.ObjCRuntime.Selector("handleGetURLEvent:withReplyEvent:"), AEEventClass.Internet,
                AEEventID.GetUrl);
            // NSAppleEventManager.SharedAppleEventManager.SetEventHandler(this,
            //     new MonoMac.ObjCRuntime.Selector("openDocumentWithContentsOfURL:display:error:"), AEEventClass.Application,
            //     AEEventID.OpenDocuments);
            // NSAppleEventManager.SharedAppleEventManager.SetEventHandler(this,
            //     new MonoMac.ObjCRuntime.Selector("openFile:"), AEEventClass.Application,
            //     AEEventID.OpenDocuments);
        }

        [Export("application:openFile:")]
        public override bool OpenFile(NSApplication sender, string filename)
        {
            _logger.Info($"Open file '{filename}'");
            return true;
        }

        [Export("application:openFiles:")]
        public override void OpenFiles(NSApplication sender, string[] filenames)
        {
            _logger.Info($"Open files '{filenames.Length}'");
        }

        [Export("application:openTempFile:")]
        public override bool OpenTempFile(NSApplication sender, string filename)
        {
            _logger.Info($"OpenTempFile '{filename}'");
            return true;
        }

        public override bool ApplicationShouldOpenUntitledFile(NSApplication sender)
        {
            _logger.Info($"ApplicationShouldOpenUntitledFile");
            return true;
        }

        public override bool OpenFileWithoutUI(NSObject sender, string filename)
        {
            _logger.Info($"OpenFileWithoutUI '{filename}'");
            return true;
        }

        public override bool ApplicationOpenUntitledFile(NSApplication sender)
        {
            _logger.Info($"ApplicationOpenUntitledFile");
            return true;
        }

        [Export("openDocumentWithContentsOfURL:display:error:")]
        private void HandleOpenDocuments(NSAppleEventDescriptor evt, NSAppleEventDescriptor replyEvent)
        {
            _logger.Info("HandleOpenDocuments");
        }
        [Export("openFile:")]
        private void OnOpenFile(NSAppleEventDescriptor evt, NSAppleEventDescriptor replyEvent)
        {
            _logger.Info("OnOpenFile");
        }

        [Export("handleGetURLEvent:withReplyEvent:")]
        private void HandleOpenURL(NSAppleEventDescriptor evt, NSAppleEventDescriptor replyEvent)
        {
            _logger.Info("Handling Open URL Event");
            // Log.Information("Handling Open URL Event");
            for (int i = 1; i <= evt.NumberOfItems; i++)
            {
                var innerDesc = evt.DescriptorAtIndex(i);

                //Grab the URL
                if (!string.IsNullOrEmpty(innerDesc.StringValue))
                {
                    //Get a hold of the Main Application View Model
                    // Log.Information($"Got URL:{innerDesc.StringValue}");
                    // this.mainWindow = AvaloniaLocator.Current.GetService<MainWindow>();
                    // var mwvm = (MainWindowViewModel)this.mainWindow.DataContext;
                    //
                    // //Get a hold of the currently loaded Model (main menu)
                    // if (mwvm.Content.GetType() == typeof(MainMenuViewModel))
                    // {
                    //     var mmvm = mwvm.Content as MainMenuViewModel;
                    //     //If there is a Loaded Identity then Invoke the Authentication Dialog
                    //     if (mmvm.CurrentIdentity != null)
                    //     {
                    //         Log.Information($"Open URL Data: {innerDesc.StringValue}");
                    //         mmvm.AuthVM = new AuthenticationViewModel(new Uri(innerDesc.StringValue));
                    //         mwvm.PriorContent = mwvm.Content;
                    //         mwvm.Content = mmvm.AuthVM;
                    //         Dispatcher.UIThread.Post(() =>
                    //         {
                    //             (MediaTypeNames.Application.Current as App).RestoreMainWindow();
                    //         });
                    //     }
                    // }
                }
            }
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
            NSRunningApplication.CurrentApplication.Activate(
                NSApplicationActivationOptions.ActivateIgnoringOtherWindows);
        }


        /// <summary>
        /// Checks the System's Environment Variable HIDIdleTime which is maintained by apple to register last Keyboard or Mouse Input
        /// </summary>
        /// <returns></returns>
        public static TimeSpan CheckIdleTime()
        {
            long idlesecs = 0;
            int iter = 0;
            TimeSpan idleTime = TimeSpan.Zero;
            if (IOServiceGetMatchingServices(0, IOServiceMatching("IOHIDSystem"), ref iter) == 0)
            {
                int entry = IOIteratorNext(iter);
                if (entry != 0)
                {
                    IntPtr dictHandle;
                    if (IORegistryEntryCreateCFProperties(entry, out dictHandle, IntPtr.Zero, 0) == 0)
                    {
                        NSDictionary dict = (NSDictionary)MonoMac.ObjCRuntime.Runtime.GetNSObject(dictHandle);
                        NSObject value;
                        dict.TryGetValue((NSString)"HIDIdleTime", out value);
                        if (value != null)
                        {
                            long nanoseconds = 0;
                            if (CFNumberGetValue(value.Handle, 4, out nanoseconds))
                            {
                                idlesecs = nanoseconds >> 30; // Shift To Convert from nanoseconds to seconds.
                                idleTime = DateTime.Now - DateTime.Now.AddSeconds(-idlesecs);
                            }
                        }
                    }

                    IOObjectRelease(entry);
                }

                IOObjectRelease(iter);
            }

            return idleTime;
        }
    }
}