namespace Picasa

open System
open System.IO
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Media
open Avalonia.Themes.Fluent
open Avalonia.Threading
open Elmish
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts
open Avalonia.Controls.ApplicationLifetimes
open Serilog
open Serilog.Events

open Picasa

open Model
open Services

(*--------------------------------------------------------------------------------------------------------------------*)

type MainWindow (args : string[], platform : Option<IPlatformServices>)  as this =
    inherit HostWindow()
    do
        let backgroundBrush = SolidColorBrush(Color.FromArgb(160uy, 0uy, 0uy, 0uy))
        base.Title <- "Picasa"
        base.WindowState <- WindowState.Maximized
        base.ShowInTaskbar <- false
        base.Background <- backgroundBrush
        base.TransparencyLevelHint <- [|
            WindowTransparencyLevel.Blur
            WindowTransparencyLevel.Transparent
        |]
        base.TransparencyBackgroundFallback <- backgroundBrush
        base.SizeToContent <- SizeToContent.Manual

        let keyListener (e : KeyEventArgs) =
            match e.Key with
            | Key.Escape -> this.Close ()
            | _ -> ()
        this.KeyDown.Add keyListener

        let imagePath =
            match args with
            | [| path; _ |]
            | [| path |] -> path
            | _ ->
                Log.Warning "Path was not provided, exiting"
                this.Close ()
                failwith "Path was not provided"

        let model = Model.initialWithCommands (Path imagePath)
        let services = Services.services platform

        let titleWasSet = ref false

        let wrappedUpdate msg model =
            if Log.Logger.IsEnabled(LogEventLevel.Verbose) then
                Log.Verbose $"Msg %A{msg}"

            let model', cmd = update services msg model
            if model.CurrentImagePath <> model'.CurrentImagePath || not titleWasSet.Value then
                titleWasSet.Value <- true
                let fileName = Path.GetFileName model'.CurrentImagePath.Value
                this.Title <- $"Picasa - {fileName}"
            (model', cmd)

        Elmish.Program.mkProgram (fun () -> model) wrappedUpdate UI.view
        |> Program.withHost this
        |> Program.withSubscription (fun _model ->
            let sub (dispatch : Dispatch<Msg>) =
                let keyDownCallback (e : KeyEventArgs) =
                    match e.Key, e.KeyModifiers with
                    | Key.Left, KeyModifiers.None -> dispatch Msg.NavigateLeft
                    | Key.Home, KeyModifiers.None
                    | Key.Left, KeyModifiers.Control -> dispatch Msg.NavigateToTheBeginning
                    | Key.Right, KeyModifiers.None -> dispatch Msg.NavigateRight
                    | Key.End, KeyModifiers.None
                    | Key.Right, KeyModifiers.Control -> dispatch Msg.NavigateToTheEnd
                    | Key.OemOpenBrackets, KeyModifiers.None -> dispatch ^ Msg.Rotate Left
                    | Key.OemCloseBrackets, KeyModifiers.None -> dispatch ^ Msg.Rotate Right
                    | Key.Back, KeyModifiers.None
                    | Key.Delete, KeyModifiers.None -> dispatch Msg.RequestDeleteCurrentImage
                    | Key.C, KeyModifiers.Meta -> dispatch Msg.RequestCopyCurrentImageToClipboard
                    | _ -> ()
                let keyDownSubscription = this.KeyDown.Subscribe keyDownCallback

                let mutable clientSize = Size()
                let layoutUpdatedHandler _ =
                    let newSize = this.ClientSize
                    if newSize <> clientSize then
                        clientSize <- newSize
                        dispatch (Msg.WindowSizeChanged newSize)
                let layoutUpdatedSubscription = this.LayoutUpdated.Subscribe layoutUpdatedHandler
                { new IDisposable with
                    member _.Dispose () =
                        keyDownSubscription.Dispose ()
                        layoutUpdatedSubscription.Dispose ()
                }

            [["Keyboard"], sub])
        |> Program.runWithAvaloniaSyncDispatch ()

type DataHolder =
    static let mutable _platformServices : Option<IPlatformServices> = None

    static member PlatformServices
        with get () = _platformServices
        and set value = _platformServices <- value

type App() as this =
    inherit Application()

    do
        NativeMenu.SetMenu(this, NativeMenu())
        this.Name <- "Picasa"

    override this.Initialize() =
        this.Styles.Add (FluentTheme ())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Light

    override this.OnFrameworkInitializationCompleted() =
        Log.Information "OnFrameworkInitializationCompleted"

        let activateWithUrl (lifetime : IClassicDesktopStyleApplicationLifetime) (url : Uri) =
            let filePath = url.ToString().Replace("file://", "")

            Dispatcher.UIThread.Post(fun () ->
                match lifetime.MainWindow |> Option.ofObj with
                | Some _ ->
                    let mainWindow = MainWindow ([| filePath |], DataHolder.PlatformServices)
                    mainWindow.Show ()
                | None ->
                    let mainWindow = MainWindow ([| filePath |], DataHolder.PlatformServices)
                    lifetime.MainWindow <- mainWindow
                    mainWindow.Show ()
            )

        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            match Application.Current.TryGetFeature(typeof<IActivatableLifetime>) |> Option.ofObj with
            | Some (:? IActivatableLifetime as activatableLifetime) ->
                activatableLifetime.Activated.Add ^ function
                    | :? ProtocolActivatedEventArgs as args ->
                        Log.Information("Activated with ProtocolActivatedEventArgs {Path}", args.Uri)
                        activateWithUrl desktopLifetime args.Uri
                    | :? FileActivatedEventArgs as args ->
                        let path = args.Files[0].Path
                        Log.Information("Activated with FileActivatedEventArgs {Path}", path)
                        activateWithUrl desktopLifetime path
                    | args ->
                        Log.Information ("Activated with args {Args}, ignoring them", args)
                        ()
            | _ ->
                Log.Warning "IActivatableLifetime not found, activation will not work as expected."
                ()

            match desktopLifetime.Args with
            | [| |] ->
                Log.Information "desktopLifetime.Args is empty, closing."
                // No file name provided, closing.
                ()
            | args ->
                let mainWindow = MainWindow (args, DataHolder.PlatformServices)
                desktopLifetime.MainWindow <- mainWindow
        | _ -> ()

module Program =

    let mainCore (args : string[]) (platform : Option<IPlatformServices>) =
        DataHolder.PlatformServices <- platform

        let logToFile = true
        let loggerConf =
            LoggerConfiguration()
              .MinimumLevel.Verbose()
              .Enrich.FromLogContext()
              .WriteTo.Console(
                outputTemplate = "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}"
              )
        let loggerConf =
            if logToFile then
                loggerConf
                    .WriteTo.File(
                        path = "/Users/mic/picasa.log",
                        outputTemplate = "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                        restrictedToMinimumLevel = LogEventLevel.Debug)
            else
                loggerConf

        Log.Logger <- loggerConf.CreateLogger ()

        try
            AppDomain.CurrentDomain.UnhandledException.Add (fun e -> Log.Error (e.ExceptionObject :?> Exception, "AppDomain.UnhandledException"))
            Log.Verbose($"Launched with args %A{args}. Command line: '%s{Environment.CommandLine}'. Args: %A{Environment.GetCommandLineArgs()}")

            match AvaloniaLocator.Current.GetService<MacOSPlatformOptions>() |> Option.ofObj with
            | Some options -> options.DisableNativeMenus <- false
            | None ->
                let options = MacOSPlatformOptions(DisableNativeMenus=false)
                AvaloniaLocator.CurrentMutable.BindToSelf options |> ignore
                Log.Debug "Registering MacOSPlatformOptions"

            match AvaloniaLocator.Current.GetService<AvaloniaNativePlatformOptions>() |> Option.ofObj with
            | Some options -> options.RenderingMode <- [| AvaloniaNativeRenderingMode.Software |]
            | None ->
                let options = AvaloniaNativePlatformOptions(RenderingMode = [| AvaloniaNativeRenderingMode.Software |])
                AvaloniaLocator.CurrentMutable.BindToSelf options |> ignore
                Log.Debug "Registering AvaloniaNativePlatformOptions"


            let exitCode =
                AppBuilder
                    .Configure<App>()
                    .UsePlatformDetect()
                    .StartWithClassicDesktopLifetime(args, ShutdownMode.OnLastWindowClose)

            Log.Information "Done"
            Log.CloseAndFlush ()

            exitCode
        with e ->
            try
                Log.Error(e, "Unhandled exception")
                Log.CloseAndFlush ()
                -1
            with e ->
                try
                    Console.WriteLine "Unhandled exception while handling an unhandled exception"
                with _ -> ()
                -2

    [<EntryPoint>]
    let main (args : string[]) =
        mainCore args None