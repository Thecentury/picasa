module Picasa.UI

open System.IO
open Avalonia
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging

type Model = {
    LeftImages : List<string>
    RightImages : List<string>
    Image : string
    WindowSize : Size
}

module Model =

    let tryMoveLeft (model : Model) =
        match model.LeftImages with
        | [] -> None
        | left :: otherLeft ->
            Some { model with
                    LeftImages = otherLeft
                    RightImages = model.Image :: model.RightImages
                    Image = left }

    let tryMoveRight (model : Model) =
        match model.RightImages with
        | [] -> None
        | right :: otherRight ->
            Some { model with
                    LeftImages = model.Image :: model.LeftImages
                    RightImages = otherRight
                    Image = right }

type Msg =
    | MoveLeft
    | MoveRight
    | WindowSizeChanged of Size

let update (msg : Msg) (model : Model) =
    match msg with
    | Msg.MoveLeft -> Model.tryMoveLeft model |> Option.defaultValue model
    | Msg.MoveRight -> Model.tryMoveRight model |> Option.defaultValue model
    | Msg.WindowSizeChanged newSize -> { model with WindowSize = newSize }

let view (model : Model) _dispatch =
    let loadImage () =
        use fs = new FileStream (model.Image, FileMode.Open, FileAccess.Read)
        if model.WindowSize.Height > 0.0 then
            Bitmap.DecodeToHeight (fs, int model.WindowSize.Height)
        else
            new Bitmap (model.Image)
    let bmp = loadImage ()

    let image =
        if bmp.Size.Width <= model.WindowSize.Width &&
           bmp.Size.Height <= model.WindowSize.Height then
            Image.create [
                Image.horizontalAlignment HorizontalAlignment.Center
                Image.verticalAlignment VerticalAlignment.Center
                Image.source bmp
                Image.renderTransform (RotateTransform(90.))
                Image.renderTransformOrigin (RelativePoint(0.5, 0.5, RelativeUnit.Relative))
            ]
        else
            Image.create [
                Image.horizontalAlignment HorizontalAlignment.Stretch
                Image.verticalAlignment VerticalAlignment.Stretch
                Image.source bmp
                Image.renderTransform (RotateTransform(90.))
                Image.renderTransformOrigin (RelativePoint(0.5, 0.5, RelativeUnit.Relative))
            ]
    Grid.create [
        Grid.children [
            image
        ]
    ]

