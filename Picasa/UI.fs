module Picasa.UI

open Avalonia
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Media.Imaging

type State = {
    Image : string
    WindowSize : Size
}

let init size = {
    Image = "C:\Downloads\E75ORggVkAITgXM.jpg"
    WindowSize = size
}

type Msg = unit

let update (msg : Msg) (model : State) =
    model

let view (model : State) dispatch =
    let bmp = new Bitmap (model.Image)
    let image =
        if bmp.Size.Width <= model.WindowSize.Width &&
           bmp.Size.Height <= model.WindowSize.Height then
            Image.create [
                Image.horizontalAlignment HorizontalAlignment.Center
                Image.verticalAlignment VerticalAlignment.Center
                Image.source bmp
            ]
        else
            Image.create [
                Image.horizontalAlignment HorizontalAlignment.Stretch
                Image.verticalAlignment VerticalAlignment.Stretch
                Image.source bmp
            ]
    Grid.create [
        Grid.children [
            image
        ]
    ]

