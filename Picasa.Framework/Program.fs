namespace Picasa

open System
open System.Diagnostics
open System.IO
open System.Threading
open AppKit
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
//open MonoMac.AppKit
open Foundation
open NLog
//open Picasa.OSX

[<Register "AppDelegate">]
type AppDelegate () =
    inherit NSApplicationDelegate ()
    
    let logger = LogManager.GetCurrentClassLogger ()

    override x.DidFinishLaunching _notification =
        logger.Info "Did finish launching"
        ()
    override x.WillTerminate _notification =
        logger.Info "WillTerminate"
        ()
        
    override x.OpenFile (_sender : NSApplication, fileName) =
        logger.Info (sprintf "Open file '%s'" fileName)
        true

    override x.OpenFiles (_sender : NSApplication, files) =
        logger.Info (sprintf "Open file '%A'" files)
        
//type MainWindow(args : string[]) as this =
//    inherit HostWindow()
//    do
//        base.Title <- "Picasa"
//        base.WindowState <- WindowState.Maximized
//        base.Background <- Brushes.Black
//
//        let keyListener (e : KeyEventArgs) =
//            match e.Key with
//            | Key.Escape -> this.Close ()
//            | _ -> ()
//        this.KeyDown.Add keyListener
////        base.SystemDecorations <- SystemDecorations.None
//
//        let imagePath =
//            match args with
//            | [| path |] -> path
//            | _ -> if Environment.OSVersion.Platform = PlatformID.MacOSX || Environment.OSVersion.Platform = PlatformID.Unix then
//                        "/Users/mic/Downloads/1587287569-c6f97fdef6db0bcbe1184a419b5eb2ac.jpeg"
//                    else
////                        "C:\Downloads\E75ORggVkAITgXM.jpg"
//                        "/Users/mic/Downloads/1587287569-c6f97fdef6db0bcbe1184a419b5eb2ac.jpeg"
//
//        let otherImages = loadOtherImages imagePath
//        let model : UI.Model = {
//            LeftImages = otherImages.Left
//            RightImages = otherImages.Right
//            Image = imagePath
//            WindowSize = this.ClientSize
//        }
//
//        let wrappedUpdate msg model =
//            let model' = UI.update msg model
//            if model.Image <> model'.Image then
//                let fileName = Path.GetFileName model'.Image
//                this.Title <- sprintf "Picasa - %s}" fileName
//            model'
//
//        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
//        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
//        Elmish.Program.mkSimple (fun () -> model) wrappedUpdate UI.view
//        |> Program.withHost this
//        |> Program.withSubscription (fun _model ->
//            let sub (dispatch : Dispatch<UI.Msg>) =
//                let keyDownCallback (e : KeyEventArgs) =
//                    match e.Key, e.KeyModifiers with
//                    | Key.Left, KeyModifiers.None -> dispatch UI.Msg.MoveLeft
//                    | Key.Right, KeyModifiers.None -> dispatch UI.Msg.MoveRight
//                    | _ -> ()
//                this.KeyDown.Add keyDownCallback
//
//                let layoutUpdatedHandler _ =
//                    dispatch (UI.Msg.WindowSizeChanged this.ClientSize)
//                this.LayoutUpdated.Add layoutUpdatedHandler
//
//            Cmd.ofSub sub)
//        |> Program.withConsoleTrace
//        |> Program.run
//
//type App() =
//    inherit Application()
//    let logger = LogManager.GetCurrentClassLogger()
//
//    override this.Initialize() =
//        this.UrlsOpened.Add (fun e -> logger.Info (sprintf "Urls opened: %A" e.Urls))
//        this.Styles.Add (FluentTheme(baseUri = null, Mode = FluentThemeMode.Light))
//
//    override this.OnFrameworkInitializationCompleted() =
//        this.UrlsOpened.Add (fun e -> logger.Info (sprintf "Urls opened: %A" e.Urls))
//        match this.ApplicationLifetime with
//        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
//            NSApplication.Init()
//            NSApplication.SharedApplication.Delegate <- new AppDelegate()
//
//            let mainWindow = MainWindow(desktopLifetime.Args)
//            desktopLifetime.MainWindow <- mainWindow
//        | _ -> ()

module Program =

    [<EntryPoint>]
    let main(args: string[]) =
        let logger = LogManager.GetCurrentClassLogger()
        try
            AppDomain.CurrentDomain.UnhandledException.Add (fun e -> logger.Error (e.ExceptionObject :?> Exception, "AppDomain.UnhandledException"))
            logger.Trace(
                sprintf "Launched with args %A. Command line: '%s'. Args: %A"
                    args
                    Environment.CommandLine
                    (Environment.GetCommandLineArgs()))
            
//            AppDelegate.InitializeToolkit()
            NSApplication.Init()
            NSApplication.SharedApplication.Delegate <- new AppDelegate()

//            let locator = AvaloniaLocator.Current :?> AvaloniaLocator
//            let opts = AvaloniaNativePlatformOptions(UseGpu = false)
//            locator.BindToSelf(opts) |> ignore
            
            let stopwatch = Stopwatch.StartNew ()
            
            while stopwatch.Elapsed.TotalSeconds < 60. do
                logger.Debug (sprintf "Waiting. Elapsed %O" stopwatch.Elapsed)
                Thread.Sleep 1000

//            let exitCode =
//                AppBuilder
//                    .Configure<App>()
//                    .UsePlatformDetect()
//                    .UseSkia()
//                    .StartWithClassicDesktopLifetime(args)
                
            logger.Info "Done"
            
            logger.Factory.Flush ()
            0
        with
        | e ->
            logger.Error(e, "Unhandled exception")
            logger.Factory.Flush ()
            -1
