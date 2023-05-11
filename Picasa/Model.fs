module Picasa.Model

open Avalonia
open Elmish

open Picasa

open Caching
open Files
open Images

(*--------------------------------------------------------------------------------------------------------------------*)

type IServices =
    abstract member DeleteImage : Path -> Async<Result<unit, string>>

type Model = {
    OtherImages : DeferredResult<SurroundingFiles>
    CurrentImagePath : Path
    CurrentImage : DeferredResult<RotatedImage>
    CachedImages : Cache<Path, Rotation, Result<RotatedImage, string>>
    WindowSize : Option<Size>
}

type Msg =
    | StartLoadingImage of Path
    | StartReadingNeighbours
    | NeighboursLoaded of Result<SurroundingFiles, string>
    | ImageLoaded of Path * Result<RotatedImage, string>
    | NavigateLeft
    | NavigateRight
    | NavigateToTheBeginning
    | NavigateToTheEnd
    | Rotate of RotationDirection
    | WindowSizeChanged of Size
    | RequestDeleteCurrentImage
    | ImageDeleted of Path * Result<unit, string>

module Model =

    let initial path =
        {
            OtherImages = HasNotStartedYet
            CurrentImagePath = path
            CurrentImage = HasNotStartedYet
            CachedImages = Cache(notMoreThanDeletionPolicy 10)
            WindowSize = None
        }

    let initialWithCommands path =
        let model = initial path
        (model, Cmd.batch [Cmd.ofMsg ^ StartLoadingImage path; Cmd.ofMsg StartReadingNeighbours])

let update (services : IServices) (msg : Msg) (model : Model) =
    match msg with
    | StartReadingNeighbours ->
        let cmd = Cmd.OfAsync.perform (runAsynchronously loadOtherImages) model.CurrentImagePath NeighboursLoaded
        { model with OtherImages = InProgress }, cmd
    | NeighboursLoaded neighbours ->
        match neighbours with
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
        | Some img, _ -> model, Cmd.ofMsg ^ ImageLoaded (path, img)
        | None, savedRotation ->
            let currentImage =
                if path = model.CurrentImagePath then
                    InProgress
                else
                    model.CurrentImage
            let cmd = Cmd.OfAsync.perform (runAsynchronously loadImage) (path, savedRotation) (fun img -> ImageLoaded (path, img))
            { model with CurrentImage = currentImage }, cmd
    | ImageLoaded (path, img) ->
        let rotation =
            match img with
            | Ok img -> img.Rotation
            | Error _ -> Rotation.NoRotation
        model.CachedImages.Add path rotation img
        let model =
            if path = model.CurrentImagePath then
                { model with CurrentImage = Resolved img }
            else
                model
        model, Cmd.none
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
            // todo mikbri update rotation in the cached image
            { model with CurrentImage = Resolved ^ Ok rotated }, Cmd.none
        | _ -> model, Cmd.none
    | RequestDeleteCurrentImage ->
        let processResult result =
            ImageDeleted (model.CurrentImagePath, result)
        let cmd = Cmd.OfAsync.perform services.DeleteImage model.CurrentImagePath processResult
        model, cmd
    | ImageDeleted (removedImage, Ok ()) ->
        model.CachedImages.Remove removedImage
        if model.CurrentImagePath = removedImage then
            match model.OtherImages with
            | ResolvedOk { Left = []; Right = [] } ->
                // todo handle deletion of the last image
                model, Cmd.none
            // Can move to the right
            | ResolvedOk { Left = ls; Right = r :: rs } ->
                let preloadNextImage = rs |> List.tryHead |> Option.map (StartLoadingImage >> Cmd.ofMsg) |> Option.toList
                let cmds = [Cmd.ofMsg ^ StartLoadingImage r] @ preloadNextImage
                let otherImages = { Left = ls; Right = rs }
                let model =
                    { model with
                        OtherImages = Resolved ^ Ok otherImages
                        CurrentImagePath = r
                        CurrentImage = InProgress }
                model, Cmd.batch cmds
            // Can move to the left
            | ResolvedOk { Left = l :: ls; Right = rs } ->
                let preloadNextImage = ls |> List.tryHead |> Option.map (StartLoadingImage >> Cmd.ofMsg) |> Option.toList
                let cmds = [Cmd.ofMsg ^ StartLoadingImage l] @ preloadNextImage
                let otherImages = { Left = ls; Right = rs }
                let model =
                    { model with
                        OtherImages = Resolved ^ Ok otherImages
                        CurrentImagePath = l
                        CurrentImage = InProgress }
                model, Cmd.batch cmds
            | _ -> model, Cmd.none
        else
            match model.OtherImages with
            | ResolvedOk otherImages -> { model with OtherImages = Resolved ^ Ok ^ removePath removedImage otherImages }, Cmd.none
            | _ -> model, Cmd.none
    | ImageDeleted (_, Error _) ->
        // todo let the user know that the image could not be deleted
        model, Cmd.none
