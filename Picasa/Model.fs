module Picasa.Model

open Avalonia.Media.Imaging
open Elmish

open Picasa.Core
open Prelude

type Model = {
    LeftImages : DeferredResult<List<Path>>
    RightImages : DeferredResult<List<Path>>
    CurrentImagePath : Path
    CurrentImage : DeferredResult<IBitmap>
    CachedImages : Map<Path, Result<IBitmap, string>>
}

type Msg =
    | StartLoadingImage of Path
    | StartReadingNeighboars
    | NeighboarsLoaded of Result<SurroundingFiles, string>
    | ImageLoaded of Path * Result<IBitmap, string>
    | NavigateLeft
    | NavigateRight

module Model =
    
    let initial path =
        {
            LeftImages = HasNotStartedYet
            RightImages = HasNotStartedYet
            CurrentImagePath = path
            CurrentImage = HasNotStartedYet
            CachedImages = Map.empty
        }
        
    let initialWithCommands path =
        let model = initial path
        (model, Cmd.batch [Cmd.ofMsg ^ StartLoadingImage path; Cmd.ofMsg StartReadingNeighboars])
        
let loadImage (Path path) =
    new Bitmap (path) :> IBitmap
    
let update (msg : Msg) (model : Model) =
    match msg with
    | StartReadingNeighboars ->
        let cmd = Cmd.OfAsync.perform (runAsynchronously loadOtherImages) model.CurrentImagePath NeighboarsLoaded
        { model with
            LeftImages = InProgress
            RightImages = InProgress }, cmd
    | NeighboarsLoaded neighboars ->
        match neighboars with
        | Ok n ->
            { model with
                LeftImages = Resolved ^ Ok n.Left
                RightImages = Resolved ^ Ok n.Right }, Cmd.none
        | Error e ->
            { model with
                LeftImages = Resolved ^ Error e
                RightImages = Resolved ^ Error e }, Cmd.none
    | StartLoadingImage path ->
        let cachedImage = model.CachedImages.TryFind path
        match cachedImage with
        | Some img -> model, Cmd.ofMsg ^ ImageLoaded (path, img)
        | None ->
            let currentImage =
                if path = model.CurrentImagePath then
                    InProgress
                else
                    model.CurrentImage
            let cmd = Cmd.OfAsync.perform (runAsynchronously loadImage) path (fun img -> ImageLoaded (path, img))
            { model with CurrentImage = currentImage }, cmd
    | ImageLoaded (path, img) ->
        let currentImage =
            if path = model.CurrentImagePath then
                Resolved img
            else
                model.CurrentImage
        let cache = Map.add path img model.CachedImages
        { model with CurrentImage = currentImage; CachedImages = cache }, Cmd.none
    | NavigateLeft ->
        match model.LeftImages, model.RightImages with
        | Resolved (Ok (l :: ls)), Resolved (Ok rs) ->
            { model with
                LeftImages = Resolved ^ Ok ls
                CurrentImagePath = l
                RightImages = Resolved ^ Ok (model.CurrentImagePath :: rs) }, Cmd.ofMsg ^ StartLoadingImage l
        | _ -> model, Cmd.none
    | NavigateRight ->
        match model.LeftImages, model.RightImages with
        | Resolved (Ok ls), Resolved (Ok (r :: rs)) ->
            { model with
                LeftImages = Resolved ^ Ok (model.CurrentImagePath :: ls)
                CurrentImagePath = r
                RightImages = Resolved ^ Ok rs }, Cmd.ofMsg ^ StartLoadingImage r
        | _ -> model, Cmd.none

            