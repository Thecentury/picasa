module Picasa.Images

open System
open System.IO
open System.Runtime.InteropServices
open Avalonia
open Avalonia.Media.Imaging
open FileFormat.Heic.Decoder

open Prelude

let private arrayAddress (arr : byte[]) =
    let handle = GCHandle.Alloc(arr, GCHandleType.Pinned)
    try
        handle.AddrOfPinnedObject ()
    finally
        handle.Free ()

let private loadHeic (Path path) =
    use fs = new FileStream(path, FileMode.Open)
    let heicImage = HeicImage.Load(fs)

    let format =
        if heicImage.DefaultFrame.HasAlpha then
            PixelFormat.Bgra32
        else
            PixelFormat.Rgb24

    let pixels = heicImage.GetByteArray(format)
    let width = int heicImage.Width
    let height = int heicImage.Height

    let dpi = Vector(72, 72)
    let pixelSize = PixelSize(width, height)
    let bpm = new WriteableBitmap(pixelSize, dpi)
    bpm.CopyPixels(PixelRect(pixelSize), arrayAddress pixels, 4 * width, 0)
    bpm :> Bitmap

let loadImage (Path path, orientation : Option<Rotation>) =
    let bmp =
        if String.Equals(Path.GetExtension(path), ".heic", StringComparison.InvariantCultureIgnoreCase) then
            loadHeic (Path path)
        else
            new Bitmap (path)
    {
        OriginalImage = bmp
        RotatedImage = bmp
        Rotation = orientation |> Option.defaultValue NoRotation
    }

let rotatePixelSize (s : PixelSize) = function
    | NoRotation
    | Right180 -> s
    | _ -> PixelSize(s.Height, s.Width)

let rotateBitmap (bmp : Bitmap) rotation =
    match rotation with
    | NoRotation -> bmp
    | other ->
        let angle = Rotation.toAngle other |> float
        let rotatedSize = rotatePixelSize bmp.PixelSize other
        let r = new RenderTargetBitmap (rotatedSize)
        use dc = r.CreateDrawingContext ()
        let correction =
            if other = Right180 then
                Matrix.Identity
            else
                let diff = float ^ abs (bmp.PixelSize.Width - bmp.PixelSize.Height)
                if bmp.PixelSize.Width < bmp.PixelSize.Height then
                    Matrix.CreateTranslation (diff * 0.5, -diff * 0.5)
                else
                    Matrix.CreateTranslation (-diff * 0.5, diff * 0.5)

        let transformMatrix =
            Matrix.CreateTranslation (float bmp.PixelSize.Width * -0.5, float bmp.PixelSize.Height * -0.5) *
            Matrix.CreateRotation (Matrix.ToRadians angle) *
            Matrix.CreateTranslation (float bmp.PixelSize.Width * 0.5, float bmp.PixelSize.Height * 0.5) *
            correction
        use _ = dc.PushTransform transformMatrix
        let rect = Rect(bmp.PixelSize.ToSize(1.))
        dc.DrawImage(bmp, rect, rect)

        dc.Dispose ()

        r

let rotateImage (img : RotatedImage) direction =
    let nextRotation = Rotation.rotate img.Rotation direction

    match img.Rotation with
    | NoRotation -> ()
    | _ -> img.RotatedImage.Dispose ()

    let rotated = rotateBitmap img.OriginalImage nextRotation
    { img with RotatedImage = rotated; Rotation = nextRotation }