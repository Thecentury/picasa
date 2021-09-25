namespace Picasa

open System
open System.IO
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

type MainWindow(args : string[]) as this =
    inherit HostWindow()
    do
        base.Title <- "Picasa"
        base.WindowState <- WindowState.Maximized
        base.Background <- Brushes.Black

        let keyListener (e : KeyEventArgs) =
            match e.Key with
            | Key.Escape -> this.Close ()
            | _ -> ()
        this.KeyDown.Add keyListener
//        base.SystemDecorations <- SystemDecorations.None

        let imagePath =
            match args with
            | [| path |] -> path
            | _ -> if Environment.OSVersion.Platform = PlatformID.MacOSX || Environment.OSVersion.Platform = PlatformID.Unix then
                        "/Users/mic/Downloads/1587287569-c6f97fdef6db0bcbe1184a419b5eb2ac.jpeg"
                    else
//                        "C:\Downloads\E75ORggVkAITgXM.jpg"
                        "/Users/mic/Downloads/1587287569-c6f97fdef6db0bcbe1184a419b5eb2ac.jpeg"

        let otherImages = loadOtherImages imagePath
        let model : UI.Model = {
            LeftImages = otherImages.Left
            RightImages = otherImages.Right
            Image = imagePath
            WindowSize = this.ClientSize
        }

        let wrappedUpdate msg model =
            let model' = UI.update msg model
            if model.Image <> model'.Image then
                let fileName = Path.GetFileName model'.Image
                this.Title <- $"Picasa - {fileName}"
            model'

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
        Elmish.Program.mkSimple (fun () -> model) wrappedUpdate UI.view
        |> Program.withHost this
        |> Program.withSubscription (fun _model ->
            let sub (dispatch : Dispatch<UI.Msg>) =
                let keyDownCallback (e : KeyEventArgs) =
                    match e.Key, e.KeyModifiers with
                    | Key.Left, KeyModifiers.None -> dispatch UI.Msg.MoveLeft
                    | Key.Right, KeyModifiers.None -> dispatch UI.Msg.MoveRight
                    | _ -> ()
                this.KeyDown.Add keyDownCallback

                let layoutUpdatedHandler _ =
                    dispatch (UI.Msg.WindowSizeChanged this.ClientSize)
                this.LayoutUpdated.Add layoutUpdatedHandler

            Cmd.ofSub sub)
        |> Program.withConsoleTrace
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

    [<EntryPoint>]
    let main(args: string[]) =
        let logger = LogManager.GetCurrentClassLogger()
        try
            logger.Trace($"Launched with args %A{args}. Command line: '%s{Environment.CommandLine}'. Args: %A{Environment.GetCommandLineArgs()}")
            let locator = AvaloniaLocator.Current :?> AvaloniaLocator
            let opts = AvaloniaNativePlatformOptions(UseGpu = false)
            locator.BindToSelf(opts) |> ignore

            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .StartWithClassicDesktopLifetime(args)
        with
        | e ->
            logger.Error(e, "Vsyo upalo")
            -1
