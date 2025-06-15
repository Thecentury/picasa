module Picasa.Services

open System.Diagnostics
open System.IO
open System.Runtime.InteropServices
open Picasa.Model
open Picasa.Prelude // For Path type
open Serilog

/// Function to escape single quotes within a string for AppleScript literal usage.
/// In AppleScript, a single quote inside a double-quoted string literal is escaped as \'
let private escapeForAppleScript (s: string) =
    s.Replace("'", "\\'")

let private deleteImageImpl (Path path) = async {
    if RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
        Log.Debug $"Deleting '{path}'"
        let escapedFilePath = escapeForAppleScript (Path.GetFullPath path)
        let startInfo = ProcessStartInfo("osascript")
        startInfo.ArgumentList.Add("-e")
        startInfo.ArgumentList.Add($"""tell app "Finder" to move {{the POSIX file "{escapedFilePath}"}} to trash""")
        startInfo.RedirectStandardError <- true
        startInfo.RedirectStandardOutput <- true
        startInfo.UseShellExecute <- false
        use p = Process.Start(startInfo)
        do! p.WaitForExitAsync () |> Async.AwaitTask
        let exitCode = p.ExitCode
        if exitCode = 0 then
            return Ok ()
        else
            let output = p.StandardOutput.ReadToEnd()
            let errorOutput = p.StandardError.ReadToEnd()
            Log.Warning $"Deleting '{path}': Exit code was not 0: {exitCode}. Output: {output}. Args: [{startInfo.Arguments}], error: {errorOutput}"
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