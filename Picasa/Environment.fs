module Picasa.Environment

open System.IO
open Avalonia
open Avalonia.Input
open Picasa.Model

let environment () =
    { new IEnvironment with
        member _.CopyToClipboard (Path fileName) img = async {
            let! formats = Application.Current.Clipboard.GetFormatsAsync() |> Async.AwaitTask
            for format in formats do
                let! data = Application.Current.Clipboard.GetDataAsync(format) |> Async.AwaitTask
                printfn $"Clipboard format: %s{format}, data: %A{data}"
                match data with
                | :? string as s -> printfn $"Clipboard string: %s{s}"
                | :? array<byte> as bytes ->
                    let filename = Path.Combine("/Users/mic/temp/Picasa/paste", format)
                    File.WriteAllBytes(filename, bytes)
                    ()
                | _ -> ()
            // let dataObject = DataObject()
            // dataObject.Set(DataFormats.FileNames, fileName)
            // dataObject.Set(DataFormats.Text, fileName)
            // do! Application.Current.Clipboard.SetDataObjectAsync dataObject |> Async.AwaitTask
            use ms = new MemoryStream()
            img.OriginalImage.Save(ms, ImageFormat.Png)
            return ()
        }
    }