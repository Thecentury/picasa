module Picasa.Clipboard

open System
open AppKit
open CoreFoundation
open Serilog

open Services

(*--------------------------------------------------------------------------------------------------------------------*)

let copyImageToClipboard (Path imagePath) =
    //DispatchQueue.MainQueue.Activate()
    //DispatchQueue.MainQueue.DispatchAsync(
    NSApplication.SharedApplication.InvokeOnMainThread(
        Action(fun () ->
            try
                let image = new NSImage(imagePath)
                if image <> null then
                    let pasteboard = NSPasteboard.GeneralPasteboard
                    pasteboard.ClearContents() |> ignore
                    let success = pasteboard.WriteObjects([| image |])

                    if success then
                        Log.Information ("Copied image {Path} to clipboard", imagePath)
                    else
                        Log.Error ("Failed to write image {Path} to clipboard", imagePath)
                else
                    Log.Error ("Could not load an image {Path}", imagePath)
            with ex ->
                Log.Error (ex, "Failed to copy image to clipboard from path: {Path}", imagePath)
        )
    )

let platformServices =
    { new IPlatformServices with
        member _.CopyToClipboard path = async {
            copyImageToClipboard path
            return Ok ()
        }
    }