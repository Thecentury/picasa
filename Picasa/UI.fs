module Picasa.UI

open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Media.Imaging
open Picasa.Model

let image (model : Model) (img : IBitmap) =
    match model.WindowSize with
    | Some size when img.Size.Width < size.Width && img.Size.Height < size.Height ->
        Image.create [
            Image.horizontalAlignment HorizontalAlignment.Center
            Image.verticalAlignment VerticalAlignment.Center
            Image.maxWidth ^ float img.PixelSize.Width
            Image.maxHeight ^ float img.PixelSize.Height
            Image.source img
        ] |> generalize
//        Grid.create [
//            Grid.horizontalAlignment HorizontalAlignment.Center
//            Grid.verticalAlignment VerticalAlignment.Center
//            Grid.children [
//                Image.create [
//                    Image.horizontalAlignment HorizontalAlignment.Center
//                    Image.verticalAlignment VerticalAlignment.Center
//                    Image.width ^ float img.PixelSize.Width
//                    Image.height ^ float img.PixelSize.Height
//                    Image.source img
//                ]
//            ]
//        ] |> generalize
    | _ ->
        Image.create [
            Image.horizontalAlignment HorizontalAlignment.Stretch
            Image.verticalAlignment VerticalAlignment.Stretch
            Image.source img
        ] |> generalize

let view (model : Model) _dispatch =
    let image =
        match model.CurrentImage with
        | Resolved (Ok img) ->
            image model img |> List.singleton
        | _ -> []

    Grid.create [
        Grid.margin 20.
        Grid.children image
    ]