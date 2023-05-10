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
open Avalonia.FuncUI.Components.Hosts
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
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
//        base.AttachDevTools ()
        
        NativeMenu.SetMenu(this, NativeMenu())
        
        let keyListener (e : KeyEventArgs) =
            match e.Key with
            | Key.Escape -> this.Close ()
            | _ -> ()
        this.KeyDown.Add keyListener
//        base.SystemDecorations <- SystemDecorations.None

        let imagePath =
            match args with
            | [| path; _ |]
            | [| path |] -> path
            | _ ->
                let logger = LogManager.GetCurrentClassLogger ()
                logger.Warn "Path was not provided, exiting"
                this.Close ()
                failwith "Path was not provided"

        let model = Model.initialWithCommands (Path imagePath)
        
        let titleWasSet = ref false

        let wrappedUpdate msg model =
            printfn $"Msg %A{msg}"
            let model', cmd = update msg model
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
                    | Key.Left, KeyModifiers.Control -> dispatch Msg.NavigateToTheBeginning
                    | Key.Right, KeyModifiers.None -> dispatch Msg.NavigateRight
                    | Key.Right, KeyModifiers.Control -> dispatch Msg.NavigateToTheEnd
                    | Key.OemOpenBrackets, KeyModifiers.None -> dispatch ^ Msg.Rotate Left
                    | Key.OemCloseBrackets, KeyModifiers.None -> dispatch ^ Msg.Rotate Right
                    | _ -> ()
                this.KeyDown.Add keyDownCallback

                let layoutUpdatedHandler _ =
                    dispatch (Msg.WindowSizeChanged this.ClientSize)
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
            
    let deleteFile (fileName : string) =
        use p = Process.Start("osascript", $"-e \"tell app \\\"Finder\\\" to move the POSIX file \\\"{fileName}\\\" to trash\"")
        p.WaitForExit ()
        Console.WriteLine $"Exit code: {p.ExitCode}"

    [<EntryPoint>]
    let main(args: string[]) =
        let fileName = "/Users/mic/Desktop/log.txt"
        let createFile () =
            if not ^ File.Exists fileName then
                use fs = File.CreateText fileName
                fs.WriteLine "Hello"
                ()
        createFile ()
        deleteFile fileName
        
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
