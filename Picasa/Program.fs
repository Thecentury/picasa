namespace Picasa

open System
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

open Core
open NLog
open Picasa
open Picasa.Model

type MainWindow(args : string[]) as this =
    inherit HostWindow()
    do
        base.Title <- "Picasa"
        base.WindowState <- WindowState.Maximized
        base.Background <- SolidColorBrush(Color.FromArgb(160uy, 0uy, 0uy, 0uy))
        base.TransparencyLevelHint <- WindowTransparencyLevel.Transparent
        base.TransparencyBackgroundFallback <- SolidColorBrush(Color.FromArgb(160uy, 0uy, 0uy, 0uy))
        base.SizeToContent <- SizeToContent.Manual
        base.AttachDevTools ()
        
        NativeMenu.SetMenu(this, NativeMenu())
        
        let keyListener (e : KeyEventArgs) =
            match e.Key with
            | Key.Escape -> this.Close ()
            | _ -> ()
        this.KeyDown.Add keyListener
        base.SystemDecorations <- SystemDecorations.None

        let imagePath =
            match args with
            | [| path |] -> path
            | _ -> if Environment.OSVersion.Platform = PlatformID.MacOSX || Environment.OSVersion.Platform = PlatformID.Unix then
//                        "/Users/mic/Downloads/avatar.jpeg"
                        "/Users/mic/Downloads/image1.jpeg"
                    else
//                        "C:\Downloads\E75ORggVkAITgXM.jpg"
//                        "/Users/mic/Downloads/avatar.jpeg"
                        "/Users/mic/Downloads/image1.jpeg"

        let model = Model.initialWithCommands (Path imagePath)
        
        let sizeWasSet = ref false

        let wrappedUpdate msg model =
            printfn $"Msg %A{msg}"
            let model', cmd = update msg model
            // todo idea display index of the current file in the dir
            if model.CurrentImagePath <> model'.CurrentImagePath || not sizeWasSet.Value then
                sizeWasSet := true
                let fileName = Path.GetFileName model'.CurrentImagePath.Value
                this.Title <- $"Picasa - {fileName}"
            (model', cmd)

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
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
//        |> Program.withConsoleTrace
        |> Program.run

type App() =
    inherit Application()
    let logger = LogManager.GetCurrentClassLogger()

    override this.Initialize() =
        this.Styles.Add (FluentTheme(baseUri = null, Mode = FluentThemeMode.Light))

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            let mainWindow = MainWindow(desktopLifetime.Args)
            desktopLifetime.MainWindow <- mainWindow
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main(args: string[]) =
        let logger = LogManager.GetCurrentClassLogger()
        try
            AppDomain.CurrentDomain.UnhandledException.Add (fun e -> logger.Error (e.ExceptionObject :?> Exception, "AppDomain.UnhandledException"))
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
        with
        | e ->
            logger.Error(e, "Unhandled exception")
            -1
