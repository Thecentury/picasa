namespace Picasa.macOS

open System
open System.Threading
open System.Threading.Tasks
open AppKit
open Avalonia

open Picasa

(*--------------------------------------------------------------------------------------------------------------------*)

module Program =

  [<CompiledName "BuildAvaloniaApp">]
  let buildAvaloniaApp () =
    AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace (areas = Array.empty)

  [<EntryPoint>]
  let main (args : string[]) =
    NSApplication.Init()

    Task.Factory.StartNew(Action(fun () -> NSApplication.SharedApplication.Run()), TaskCreationOptions.LongRunning) |> ignore
    Picasa.Program.mainCore args (Some Clipboard.platformServices)