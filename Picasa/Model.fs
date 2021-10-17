module Picasa.Model

open Avalonia
open Elmish

open Picasa

open Files
open Images
    
type Model = {
    OtherImages : DeferredResult<SurroundingFiles>
    CurrentImagePath : Path
    CurrentImage : DeferredResult<RotatedImage>
    CachedImages : Map<Path, Result<RotatedImage, string>>
    WindowSize : Option<Size>
}

type Msg =
    | StartLoadingImage of Path
    | StartReadingNeighboars
    | NeighboarsLoaded of Result<SurroundingFiles, string>
    | ImageLoaded of Path * Result<RotatedImage, string>
    | NavigateLeft
    | NavigateRight
    | NavigateToTheBeginning
    | NavigateToTheEnd
    | Rotate of RotationDirection
    | WindowSizeChanged of Size

module Model =
    
    let initial path =
        {
            OtherImages = HasNotStartedYet
            CurrentImagePath = path
            CurrentImage = HasNotStartedYet
            CachedImages = Map.empty
            WindowSize = None
        }
        
    let initialWithCommands path =
        let model = initial path
        (model, Cmd.batch [Cmd.ofMsg ^ StartLoadingImage path; Cmd.ofMsg StartReadingNeighboars])
            
let update (msg : Msg) (model : Model) =
    match msg with
    | StartReadingNeighboars ->
        let cmd = Cmd.OfAsync.perform (runAsynchronously loadOtherImages) model.CurrentImagePath NeighboarsLoaded
        { model with OtherImages = InProgress }, cmd
    | NeighboarsLoaded neighboars ->
        match neighboars with
        | Ok n ->
            let preload =
                [n.Left |> List.tryHead; n.Right |> List.tryHead]
                |> List.map Option.toList |> List.collect id
                |> List.map (StartLoadingImage >> Cmd.ofMsg)
            { model with OtherImages = Resolved ^ Ok n }, Cmd.batch preload
        | Error e ->
            { model with OtherImages = Resolved ^ Error e }, Cmd.none
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
        match model.OtherImages with
        | ResolvedOk { Left = l :: ls; Right = rs } ->
            let preloadNextImage = ls |> List.tryHead |> Option.map (StartLoadingImage >> Cmd.ofMsg) |> Option.toList
            let cmds = [Cmd.ofMsg ^ StartLoadingImage l] @ preloadNextImage
            let otherImages = { Left = ls; Right = model.CurrentImagePath :: rs }
            let model =
                { model with
                    OtherImages = Resolved ^ Ok otherImages
                    CurrentImagePath = l
                    CurrentImage = Deferred.InProgress }
            model, Cmd.batch cmds
        | _ -> model, Cmd.none
    | NavigateToTheBeginning ->
        match model.OtherImages with
        | ResolvedOk { Left = ls; Right = rs } ->
            let first = ls |> List.tryLast
            match first with
            | Some first ->
                let other = ls |> List.take (ls.Length - 1) |> List.rev
                let commands = [
                    Cmd.ofMsg ^ StartLoadingImage first
                    match List.tryHead other with
                    | None -> ()
                    | Some next -> Cmd.ofMsg ^ StartLoadingImage next
                ]
                let otherImages = { Left = []; Right = other @ model.CurrentImagePath :: rs }
                { model with
                    OtherImages = Resolved ^ Ok otherImages
                    CurrentImagePath = first
                    CurrentImage = InProgress },
                Cmd.batch commands
            | None ->
                model, Cmd.none
        | _ -> model, Cmd.none
    | NavigateRight ->
        match model.OtherImages with
        | ResolvedOk { Left = ls; Right = r :: rs } ->
            let preloadNextImage = rs |> List.tryHead |> Option.map (StartLoadingImage >> Cmd.ofMsg) |> Option.toList
            let cmds = [Cmd.ofMsg ^ StartLoadingImage r] @ preloadNextImage
            let otherImages = { Left = model.CurrentImagePath :: ls; Right = rs }
            let model =
                { model with
                    OtherImages = Resolved ^ Ok otherImages
                    CurrentImagePath = r
                    CurrentImage = InProgress }
            model, Cmd.batch cmds
        | _ -> model, Cmd.none
    | NavigateToTheEnd ->
        match model.OtherImages with
        | ResolvedOk { Left = ls; Right = rs } ->
            let last = rs |> List.tryLast
            match last with
            | Some last ->
                let other = rs |> List.take (rs.Length - 1) |> List.rev
                let commands = [
                    Cmd.ofMsg ^ StartLoadingImage last
                    match List.tryHead other with
                    | None -> ()
                    | Some next -> Cmd.ofMsg ^ StartLoadingImage next
                ]
                let orderImages = { Left = other @ (model.CurrentImagePath :: ls); Right = [] }
                { model with
                    OtherImages = Resolved ^ Ok orderImages
                    CurrentImagePath = last
                    CurrentImage = InProgress }, Cmd.batch commands
            | None ->
                model, Cmd.none
        | _ -> model, Cmd.none
    | WindowSizeChanged newSize ->
        if newSize.IsDefault then
            model, Cmd.none
        else if Some newSize = model.WindowSize then
            model, Cmd.none
        else
            let model' = { model with WindowSize = Some newSize }
            model', Cmd.none
    | Rotate dir ->
        match model.CurrentImage with
        | Resolved (Ok img) ->
            let rotated = rotateImage img dir
            { model with CurrentImage = Resolved ^ Ok rotated }, Cmd.none
        | _ -> model, Cmd.none            