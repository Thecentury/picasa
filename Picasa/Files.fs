module Picasa.Files

open System
open System.IO
open Prelude

let private filters = [
    "*.jpg"
    "*.jpeg"
    "*.bmp"
    "*.png"
    "*.webp"
]

type SurroundingFiles = {
    Left : List<Path>
    Right : List<Path>
}

let loadOtherImages (Path current) = 
    let dir = Path.GetDirectoryName current
    let initialState = {
        Left = []
        Right = []
    }

    let folder (state : SurroundingFiles) (file : string) =
        let comparison = String.Compare (file, current)
        match comparison with
        | c when c < 0 ->
            { Left = (Path file) :: state.Left
              Right = state.Right }
        | c when c = 0 -> state
        | _ ->
            { Left = state.Left
              Right = (Path file) :: state.Right }
            
    let otherImages =
        filters
        |> Seq.collect (fun f -> Directory.EnumerateFiles (dir, f, EnumerationOptions(MatchCasing = MatchCasing.CaseInsensitive)))
        |> Seq.sortBy (fun s -> s.ToLowerInvariant())
        |> Seq.toList

    let otherImages =
        otherImages
        |> Seq.fold folder initialState

    { otherImages with Right = List.rev otherImages.Right }