namespace Picasa.macOS

open Avalonia

open Picasa

(*--------------------------------------------------------------------------------------------------------------------*)

module Program =

  [<CompiledName "BuildAvaloniaApp">]
  let buildAvaloniaApp () =
    AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace (areas = Array.empty)

  [<EntryPoint>]
  let main (args : string[]) =
    Program.main args