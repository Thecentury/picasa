namespace OSM

open System
open System.Globalization
open System.Threading
open Avalonia
open Avalonia.Controls
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.Controls.ApplicationLifetimes
open CoreLocation
open Serilog
open Serilog.Exceptions
open Serilog.Sinks.SystemConsole.Themes

(*--------------------------------------------------------------------------------------------------------------------*)

type MainWindow() =
  inherit HostWindow()

  do
     base.Title <- "OSM Navigator"
  // base.WindowState <- WindowState.Maximized
  // base.Height <- 400.0
  // base.Width <- 400.0

  //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
     // base.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true

type App() =
  inherit Application()

  let locationManager = new CLLocationManager()

  override this.Initialize () =
    this.Styles.Add (FluentTheme ())
    this.RequestedThemeVariant <- Styling.ThemeVariant.Light

  override this.OnFrameworkInitializationCompleted () =
    // location.AllowsBackgroundLocationUpdates <- true
    locationManager.RequestWhenInUseAuthorization()
    locationManager.PausesLocationUpdatesAutomatically <- true
    locationManager.Purpose <- "OSM Navigator"
    locationManager.ShowsBackgroundLocationIndicator <- true
    locationManager.AuthorizationChanged.Add(fun args ->
        Log.Debug("Authorization: {status}", args.Status)
        if args.Status = CLAuthorizationStatus.AuthorizedWhenInUse then
            locationManager.StartUpdatingLocation()
            locationManager.StartUpdatingHeading()
    )
    locationManager.LocationsUpdated.Add(fun args ->
        let location = args.Locations[0]
        let lat = location.Coordinate.Latitude
        let lon = location.Coordinate.Longitude
        Log.Debug("Location: {lat}, {lon}", lat, lon)
    )
    locationManager.UpdatedHeading.Add(fun args ->
        let heading = args.NewHeading
        Log.Debug("Heading: {TrueHeading}", heading.TrueHeading)
    )
    locationManager.LocationUpdatesPaused.Add(fun _ ->
        Log.Debug("Location updates paused")
    )
    locationManager.LocationUpdatesResumed.Add(fun _ ->
        Log.Debug("Location updates resumed")
    )
    let status = CLLocationManager.Status
    if status = CLAuthorizationStatus.AuthorizedWhenInUse then
      locationManager.StartUpdatingLocation()
      locationManager.StartUpdatingHeading()

    match this.ApplicationLifetime with
    | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
      let mainWindow = MainWindow ()
      #if DEBUG
      mainWindow.AttachDevTools()
      #endif
      // let actions = {
      //   GlobalKeyPressEvent = Some mainWindow.KeyDown
      //   Shutdown = Some ^ fun () -> desktopLifetime.Shutdown()
      //   LocationChangedEvent = None
      //   HeadingChangedEvent = None
      //   ResourcesFolder = "Contents/Resources/"
      // }
      // mainWindow.Content <- OSMView.view actions
      desktopLifetime.MainWindow <- mainWindow
      desktopLifetime.ShutdownMode <- ShutdownMode.OnMainWindowClose
    | _ -> ()

module Program =

  [<CompiledName "BuildAvaloniaApp">]
  let buildAvaloniaApp () =
    AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace (areas = Array.empty)

  [<EntryPoint>]
  let main (args : string[]) =
    // todo
    Picasa.Program.main args
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
