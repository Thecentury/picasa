module Picasa.UI

open System
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Transformation
open Picasa.Model
open Picasa.AvaloniaExtensions

let view (model : Model) _dispatch =
    let image =
        match model.CurrentImage with
        | Resolved (Ok img) ->
            let image = Image.create [
                Image.horizontalAlignment HorizontalAlignment.Stretch
                Image.verticalAlignment VerticalAlignment.Stretch
                Image.maxWidth ^ float img.PixelSize.Width
                Image.maxHeight ^ float img.PixelSize.Height
                Image.source img
            ]
            let transform =
                let builder = TransformOperations.Builder(1)
                builder.AppendRotate(Math.PI / 4. * 2.)
                builder.Build()
            LayoutTransformControl.create [
                LayoutTransformControl.layoutTransform (RotateTransform(float model.RotationAngle, 0.5 * float img.PixelSize.Width, 0.5 * float img.PixelSize.Height))
//                LayoutTransformControl.layoutTransform transform
                LayoutTransformControl.child image
            ]
             |> generalize
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
            match model.LeftImages, model.RightImages with
            | Resolved (Ok left), Resolved (Ok right) ->
                $"{left.Length + 1} / {left.Length + 1 + right.Length} — "
            | _ -> ""
            
        let fullCaption =
            match model.CurrentImage with
            | Resolved (Ok img) -> $"{fileName} - {img.PixelSize.Width} × {img.PixelSize.Height}"
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
