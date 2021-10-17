module Picasa.Model

open Avalonia
open Avalonia.Media.Imaging
open Elmish

open Picasa.Core
open Prelude

type Rotation =
    | NoRotation
    | Right90
    | Right180
    | Right270
    
type RotationDirection = Left | Right
    
module Rotation =
    
    let rotateRight = function
        | NoRotation -> Right90
        | Right90 -> Right180
        | Right180 -> Right270
        | Right270 -> NoRotation

    let rotateLeft = function
        | NoRotation -> Right270
        | Right90 -> NoRotation
        | Right180 -> Right90
        | Right270 -> Right180
        
    let rotate rotation dir =
        match dir with
        | Left -> rotateLeft rotation
        | Right -> rotateRight rotation
        
    let toAngle = function
        | NoRotation -> 0
        | Right90 -> 90
        | Right180 -> 180
        | Right270 -> 270

type RotatedImage = {
    OriginalImage : IBitmap
    RotatedImage : IBitmap
    Rotation : Rotation
}

let loadImage (Path path) =
    let bmp = new Bitmap (path) :> IBitmap
    {
        OriginalImage = bmp
        RotatedImage = bmp
        Rotation = NoRotation
    }
    
let rotatePixelSize (s : PixelSize) = function
    | NoRotation
    | Right180 -> s
    | _ -> PixelSize(s.Height, s.Width)

let rotateBitmap (bmp : IBitmap) rotation =
    match rotation with
    | NoRotation -> bmp
    | other ->
        let angle = Rotation.toAngle other |> float
        let rotatedSize = rotatePixelSize bmp.PixelSize other
        let r = new RenderTargetBitmap (rotatedSize)
        use dc = r.CreateDrawingContext null
        let correction =
            if other = Right180 then Matrix.Identity else 
            let diff = float ^ abs (bmp.PixelSize.Width - bmp.PixelSize.Height)
            Matrix.CreateTranslation (-diff * 0.5, diff * 0.5)
        dc.Transform <-
            Matrix.CreateTranslation (float bmp.PixelSize.Width * -0.5, float bmp.PixelSize.Height * -0.5) *
            Matrix.CreateRotation (Matrix.ToRadians angle) *
            Matrix.CreateTranslation (float bmp.PixelSize.Width * 0.5, float bmp.PixelSize.Height * 0.5) *
            correction
        let rect = Rect(bmp.PixelSize.ToSize(1.))
        dc.DrawBitmap (bmp.PlatformImpl, 1.0, rect, rect)
        dc.Dispose ()
        
        r :> IBitmap
        
let rotateImage (img : RotatedImage) direction =
    let nextRotation = Rotation.rotate img.Rotation direction

    match img.Rotation with
    | NoRotation -> ()
    | _ -> img.RotatedImage.Dispose ()

    let rotated = rotateBitmap img.OriginalImage nextRotation
    { img with RotatedImage = rotated; Rotation = nextRotation }

type Model = {
    LeftImages : DeferredResult<List<Path>>
    RightImages : DeferredResult<List<Path>>
    CurrentImagePath : Path
    CurrentImage : DeferredResult<RotatedImage>
    CachedImages : Map<Path, Result<RotatedImage, string>>
    WindowSize : Option<Size>
} with
    override this.ToString () = $"Model '%s{this.CurrentImagePath.Value}'"

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
            LeftImages = HasNotStartedYet
            RightImages = HasNotStartedYet
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
        { model with
            LeftImages = InProgress
            RightImages = InProgress }, cmd
    | NeighboarsLoaded neighboars ->
        match neighboars with
        | Ok n ->
            let preload =
                [n.Left |> List.tryHead; n.Right |> List.tryHead]
                |> List.map Option.toList |> List.collect id
                |> List.map (StartLoadingImage >> Cmd.ofMsg)
            { model with
                LeftImages = Resolved ^ Ok n.Left
                RightImages = Resolved ^ Ok n.Right }, Cmd.batch preload
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
            let preloadNextImage = ls |> List.tryHead |> Option.map (StartLoadingImage >> Cmd.ofMsg) |> Option.toList
            let cmds = [Cmd.ofMsg ^ StartLoadingImage l] @ preloadNextImage
            let model =
                { model with
                    LeftImages = Resolved ^ Ok ls
                    CurrentImagePath = l
                    CurrentImage = Deferred.InProgress
                    RightImages = Resolved ^ Ok (model.CurrentImagePath :: rs) }
            model, Cmd.batch cmds
        | _ -> model, Cmd.none
    | NavigateToTheBeginning ->
        match model.LeftImages, model.RightImages with
        | Resolved (Ok ls), Resolved (Ok rs) ->
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
                { model with
                    LeftImages = Resolved ^ Ok []
                    CurrentImagePath = first
                    CurrentImage = InProgress
                    RightImages = Resolved ^ Ok (other @ model.CurrentImagePath :: rs) },
                Cmd.batch commands
            | None ->
                model, Cmd.none
        | _ -> model, Cmd.none
    | NavigateRight ->
        match model.LeftImages, model.RightImages with
        | Resolved (Ok ls), Resolved (Ok (r :: rs)) ->
            let preloadNextImage = rs |> List.tryHead |> Option.map (StartLoadingImage >> Cmd.ofMsg) |> Option.toList
            let cmds = [Cmd.ofMsg ^ StartLoadingImage r] @ preloadNextImage
            let model =
                { model with
                    LeftImages = Resolved ^ Ok (model.CurrentImagePath :: ls)
                    CurrentImagePath = r
                    CurrentImage = InProgress
                    RightImages = Resolved ^ Ok rs }
            model, Cmd.batch cmds
        | _ -> model, Cmd.none
    | NavigateToTheEnd ->
        match model.LeftImages, model.RightImages with
        | Resolved (Ok ls), Resolved (Ok rs) ->
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
                { model with
                    LeftImages = Resolved ^ Ok (other @ (model.CurrentImagePath :: ls))
                    CurrentImagePath = last
                    CurrentImage = InProgress
                    RightImages = Resolved ^ Ok [] }, Cmd.batch commands
            | None ->
                model, Cmd.none
        | _ -> model, Cmd.none
    | WindowSizeChanged newSize ->
        if newSize.IsDefault then
            model, Cmd.none
        else
            { model with WindowSize = Some newSize }, Cmd.none
    | Rotate dir ->
        match model.CurrentImage with
        | Resolved (Ok img) ->
            let rotated = rotateImage img dir
            { model with CurrentImage = Resolved ^ Ok rotated }, Cmd.none
        | _ -> model, Cmd.none            