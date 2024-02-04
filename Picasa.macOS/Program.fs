namespace OSM

open Avalonia

open Picasa

(*--------------------------------------------------------------------------------------------------------------------*)

module Program =

  [<CompiledName "BuildAvaloniaApp">]
  let buildAvaloniaApp () =
    AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace (areas = Array.empty)

  [<EntryPoint>]
  let main (args : string[]) =
    Program.main args
    // try
    //   Log.Logger <- LoggerConfiguration()
    //     .MinimumLevel.Verbose()
    //     .Enrich.FromLogContext()
    //     .Enrich.WithExceptionDetails()
    //     .WriteTo.Console(
    //       outputTemplate = "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}",
    //       theme = ConsoleTheme.None
    //     )
    //     .CreateLogger()
    //
    //   Thread.CurrentThread.CurrentCulture <- CultureInfo.InvariantCulture
    //   Thread.CurrentThread.CurrentUICulture <- CultureInfo.InvariantCulture
    //
    // // let opts = AvaloniaNativePlatformOptions (UseGpu = false)
    // // AvaloniaLocator.CurrentMutable.BindToSelf opts |> ignore
    //
    //   Log.Information("Starting application")
    //   Console.WriteLine("Starting application")
    //   AppBuilder.Configure<App>().UsePlatformDetect().UseSkia().UseAvaloniaNative().StartWithClassicDesktopLifetime args
    // with e ->
    //   Log.Fatal (e, "Fatal error")
    //   1
