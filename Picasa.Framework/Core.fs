module Picasa.Core
//
//open System
//open System.IO
//
//let private filters = [
//    "*.jpg"
//    "*.jpeg"
//    "*.bmp"
//    "*.png"
//    "*.tiff"
//]
//
//type SurroundingFiles = {
//    Left : List<string>
//    Right : List<string>
//}
//
//let loadOtherImages (current : string) =
//    let dir = Path.GetDirectoryName current
//    let initialState = {
//        Left = []
//        Right = []
//    }
//
//    let folder (state : SurroundingFiles) (file : string) =
//        let comparison = String.Compare (file, current)
//        match comparison with
//        | c when c < 0 ->
//            { Left = file :: state.Left
//              Right = state.Right }
//        | c when c = 0 -> state
//        | _ ->
//            { Left = state.Left
//              Right = file :: state.Right }
//
//    let otherImages =
//        filters
//        |> Seq.collect (fun f -> Directory.EnumerateFiles (dir, f, SearchOption.TopDirectoryOnly))
//        |> Seq.sort
//        |> Seq.toList
//
//    let otherImages =
//        otherImages
//        |> Seq.fold folder initialState
//
//    { otherImages with Right = List.rev otherImages.Right }
//
