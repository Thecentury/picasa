using AppKit;

namespace Picasa
{
    internal static class MainClass
    {
        public static void Main(string[] args)
        {
            NSApplication.Init();
            NSApplication.SharedApplication.Delegate = new AppDelegate();
            NSApplication.Main(args);
        }
    }
}
