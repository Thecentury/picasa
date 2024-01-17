namespace Picasa

open System
open System.Diagnostics
open System.IO
open Prelude
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Media
open Avalonia.Themes.Fluent
open Elmish
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Hosts
open Avalonia.Controls.ApplicationLifetimes

open NLog
open Picasa
open Picasa.Model

type MainWindow(args : string[]) as this =
    inherit HostWindow()
    do
        let backgroundBrush = SolidColorBrush(Color.FromArgb(160uy, 0uy, 0uy, 0uy))
        base.Title <- "Picasa"
        base.WindowState <- WindowState.Maximized
        base.ShowInTaskbar <- false
        base.Background <- backgroundBrush
        base.TransparencyLevelHint <- WindowTransparencyLevel.Transparent
        base.TransparencyBackgroundFallback <- backgroundBrush
        base.SizeToContent <- SizeToContent.Manual
        let logger = LogManager.GetCurrentClassLogger()

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
                logger.Warn "Path was not provided, exiting"
                this.Close ()
                failwith "Path was not provided"

        let model = Model.initialWithCommands (Path imagePath)
        let services = Services.services ()

        let titleWasSet = ref false

        let wrappedUpdate msg model =
            if logger.IsTraceEnabled then
                logger.Trace $"Msg %A{msg}"

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
                this.KeyDown.Add keyDownCallback

                let mutable clientSize = Size()
                let layoutUpdatedHandler _ =
                    let newSize = this.ClientSize
                    if newSize <> clientSize then
                        clientSize <- newSize
                        dispatch (Msg.WindowSizeChanged newSize)
                this.LayoutUpdated.Add layoutUpdatedHandler

            Cmd.ofSub sub)
        |> Program.run

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add (FluentTheme(baseUri = null, Mode = FluentThemeMode.Light))

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            let mainWindow = MainWindow(desktopLifetime.Args)
            desktopLifetime.MainWindow <- mainWindow
        | _ -> ()


module Program =

    let logger = LogManager.GetCurrentClassLogger()

    let closeParent () =
        try
            let args = Environment.GetCommandLineArgs()
            if args.Length >= 3 then
                let parsed, id = Int32.TryParse args[2]
                if parsed then
                    use parent = Process.GetProcessById(id)
                    parent.Kill ()
                    logger.Info $"Killed parent by PID '{id}'"
                else
                    logger.Info $"Failed to parse parent PID '{args[2]}' as int"
        with
        | e ->
            logger.Error(e, "Failed to kill parent process")

    [<EntryPoint>]
    let main(args: string[]) =
        try
            try
                AppDomain.CurrentDomain.UnhandledException.Add (fun e -> logger.Error (e.ExceptionObject :?> Exception, "AppDomain.UnhandledException"))
                AppDomain.CurrentDomain.ProcessExit.Add (fun _ -> closeParent ())
                logger.Trace($"Launched with args %A{args}. Command line: '%s{Environment.CommandLine}'. Args: %A{Environment.GetCommandLineArgs()}")

                let locator = AvaloniaLocator.Current :?> AvaloniaLocator
                let opts = AvaloniaNativePlatformOptions(UseGpu = false)
                locator.BindToSelf(opts) |> ignore

                let exitCode =
                    AppBuilder
                        .Configure<App>()
                        .UsePlatformDetect()
                        .UseSkia()
                        .StartWithClassicDesktopLifetime(args)

                logger.Info "Done"

                exitCode
            finally
                closeParent ()
        with
        | e ->
            logger.Error(e, "Unhandled exception")
            -1
