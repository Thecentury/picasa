namespace Picasa

open System
open System.IO
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Media
open Avalonia.Themes.Fluent
open Elmish
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts
open Avalonia.Controls.ApplicationLifetimes
open Serilog

open Picasa

open Model
open Serilog.Events

(*--------------------------------------------------------------------------------------------------------------------*)

type MainWindow(args : string[]) as this =
    inherit HostWindow()
    do
        let backgroundBrush = SolidColorBrush(Color.FromArgb(160uy, 0uy, 0uy, 0uy))
        base.Title <- "Picasa"
        base.WindowState <- WindowState.Maximized
        base.ShowInTaskbar <- false
        base.Background <- backgroundBrush
        base.TransparencyLevelHint <- [|
            WindowTransparencyLevel.AcrylicBlur
            WindowTransparencyLevel.Blur
            WindowTransparencyLevel.Transparent
        |]
        base.TransparencyBackgroundFallback <- backgroundBrush
        base.SizeToContent <- SizeToContent.Manual

        NativeMenu.SetMenu(this, NativeMenu())

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
        let services = Services.services ()

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
                    | Key.Delete, KeyModifiers.None -> dispatch ^ Msg.RequestDeleteCurrentImage
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
        |> Program.run

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add (FluentTheme ())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Light

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IActivatableApplicationLifetime as activatable & (:? IClassicDesktopStyleApplicationLifetime as desktopLifetime) ->
            activatable.Activated.Add ^ function
                | :? ProtocolActivatedEventArgs as args ->
                    let filePath = args.Uri.ToString().Replace("file://", "")
                    Log.Information($"Activated with {filePath}")

                    match desktopLifetime.MainWindow |> Option.ofObj with
                    | Some _ ->
                        let mainWindow = MainWindow [| filePath |]
                        mainWindow.Show ()
                    | None ->
                        let mainWindow = MainWindow [| filePath |]
                        desktopLifetime.MainWindow <- mainWindow
                | _ -> ()
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            match desktopLifetime.Args with
            | [| |] ->
                // No file name provided, closing.
                ()
            | args ->
                let mainWindow = MainWindow args
                desktopLifetime.MainWindow <- mainWindow
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main(args: string[]) =
        Log.Logger <- LoggerConfiguration()
          .MinimumLevel.Verbose()
          .Enrich.FromLogContext()
          .WriteTo.Console(
            outputTemplate = "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}"
          )
          .WriteTo.File(path="/Users/mic/picasa.log", outputTemplate="[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
          .CreateLogger()

        try
            AppDomain.CurrentDomain.UnhandledException.Add (fun e -> Log.Error (e.ExceptionObject :?> Exception, "AppDomain.UnhandledException"))
            Log.Verbose($"Launched with args %A{args}. Command line: '%s{Environment.CommandLine}'. Args: %A{Environment.GetCommandLineArgs()}")

            // todo
            // let locator = AvaloniaLocator.Current :?> AvaloniaLocator
            // let opts = AvaloniaNativePlatformOptions(UseGpu = false)
            // locator.BindToSelf(opts) |> ignore

            let exitCode =
                AppBuilder
                    .Configure<App>()
                    .UsePlatformDetect()
                    .StartWithClassicDesktopLifetime(args)

            Log.Information "Done"
            Log.CloseAndFlush ()

            exitCode
        with
        | e ->
            Log.Error(e, "Unhandled exception")
            Log.CloseAndFlush ()
            -1
