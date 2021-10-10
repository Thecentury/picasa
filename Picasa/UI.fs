module Picasa.UI

open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Picasa.Model

let view (model : Model) _dispatch =
    let image =
        match model.CurrentImage with
        | Resolved (Ok img) ->
            Image.create [
                Image.horizontalAlignment HorizontalAlignment.Stretch
                Image.verticalAlignment VerticalAlignment.Stretch
                Image.maxWidth ^ float img.PixelSize.Width
                Image.maxHeight ^ float img.PixelSize.Height
                Image.source img
            ] |> generalize |> List.singleton
        | _ -> []

    Grid.create [
        Grid.margin 20.
        Grid.children image
    ]