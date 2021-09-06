namespace Picasa

open Avalonia
open Avalonia.Controls
open Avalonia.Themes.Fluent
open Elmish
open Avalonia.FuncUI.Components.Hosts
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open Avalonia.Controls.ApplicationLifetimes

type MainWindow(args : string[]) as this =
    inherit HostWindow()
    do
        base.Title <- "Picasa"
        base.WindowState <- WindowState.Maximized
        this.LayoutUpdated.Add(fun x ->
            let w = this
            let s = this.Width
            ())
        let width = this.Width
//        base.SystemDecorations <- SystemDecorations.None

        let imagePath =
            match args with
            | [| path |] -> path
            | _ -> "C:\Downloads\E75ORggVkAITgXM.jpg"

        let model : UI.State = {
            Image = imagePath
            WindowSize = this.ClientSize
        }

        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
        Elmish.Program.mkSimple (fun () -> model) UI.update UI.view
        |> Program.withHost this
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
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
