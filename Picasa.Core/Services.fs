module Picasa.Services

open System.Diagnostics
open System.IO
open System.Runtime.InteropServices
open Picasa.Model
open Picasa.Prelude // For Path type
open Serilog

let private deleteImageImpl (Path path) = async {
    if RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
        Log.Debug $"Deleting '{path}'"
        use p = Process.Start("osascript", $"-e \"tell app \\\"Finder\\\" to move the POSIX file \\\"{path}\\\" to trash\"")
        do! p.WaitForExitAsync () |> Async.AwaitTask
        let exitCode = p.ExitCode
        if exitCode = 0 then
            return Ok ()
        else
            Log.Warning $"Deleting '{path}': Exit code was not 0: {exitCode}"
            return Error "Failed to delete the image"
    else
        return Error "Delete not implemented for this platform"
}

let private deleteImage (path : Path) = async {
    do! Async.SwitchToThreadPool ()
    try
        if File.Exists path.Value then
            return! deleteImageImpl path
        else
            return Error "File does not exist"
    with e ->
        Log.Error(e, $"Failed to delete '{path.Value}'")
        return Error $"{e.GetType().Name}: {e.Message}"
}

let services () =

    { new IServices with
        member _.DeleteImage path = deleteImage path }