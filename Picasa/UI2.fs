module Picasa.UI2

open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Picasa.Model

let view (model : Model) _dispatch =
    // todo do not stretch if image is smaller than window
    let image =
//        if bmp.Size.Width <= model.WindowSize.Width &&
//           bmp.Size.Height <= model.WindowSize.Height then
//            Image.create [
//                Image.horizontalAlignment HorizontalAlignment.Center
//                Image.verticalAlignment VerticalAlignment.Center
//                Image.source bmp
////                Image.renderTransform (RotateTransform(90.))
////                Image.renderTransformOrigin (RelativePoint(0.5, 0.5, RelativeUnit.Relative))
//            ]
//        else
        match model.CurrentImage with
        | Resolved (Ok img) ->
            Image.create [
                Image.horizontalAlignment HorizontalAlignment.Stretch
                Image.verticalAlignment VerticalAlignment.Stretch
                Image.source img
            ] |> generalize |> List.singleton
        | _ -> []
    Grid.create [
        Grid.children image
    ]