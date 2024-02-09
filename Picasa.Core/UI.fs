module Picasa.UI

open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Media
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Picasa.Model

let view (model : Model) _dispatch =
    let image =
        match model.CurrentImage with
        | Resolved (Ok img) ->
            Image.create [
                Image.horizontalAlignment HorizontalAlignment.Stretch
                Image.verticalAlignment VerticalAlignment.Stretch
                Image.maxWidth ^ float img.RotatedImage.PixelSize.Width
                Image.maxHeight ^ float img.RotatedImage.PixelSize.Height
                Image.source img.RotatedImage
            ] |> generalize
        | Resolved (Error e) ->
            TextBlock.create [
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.fontSize 20.
                TextBlock.text e
                TextBlock.foreground Brushes.White
            ] |> generalize
        | _ ->
            ProgressBar.create [
                ProgressBar.verticalAlignment VerticalAlignment.Center
                ProgressBar.horizontalAlignment HorizontalAlignment.Center
                ProgressBar.isIndeterminate true
                ProgressBar.width 100.
            ] |> generalize

    let caption =
        let fileName = System.IO.Path.GetFileName model.CurrentImagePath.Value

        let position =
            match model.OtherImages with
            | ResolvedOk { Left = left; Right = right } ->
                $"{left.Length + 1} / {left.Length + 1 + right.Length} — "
            | _ -> ""

        let fullCaption =
            match model.CurrentImage with
            | Resolved (Ok img) -> $"{fileName} - {img.OriginalImage.PixelSize.Width} × {img.OriginalImage.PixelSize.Height}"
            | _ -> fileName
        let fullCaption = position + fullCaption

        TextBlock.create [
            TextBlock.text fullCaption
            TextBlock.horizontalAlignment HorizontalAlignment.Center
            TextBlock.foreground Brushes.White
            TextBlock.margin (0., 5., 0., 0.)
            DockPanel.dock Dock.Bottom
        ] |> generalize

    DockPanel.create [
        DockPanel.horizontalAlignment HorizontalAlignment.Stretch
        DockPanel.verticalAlignment VerticalAlignment.Stretch
        DockPanel.margin (20., 20., 20., 5.)
        DockPanel.lastChildFill true
        DockPanel.children [
            caption
            image
        ]
    ]
