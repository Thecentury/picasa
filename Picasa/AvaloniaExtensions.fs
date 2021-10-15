module Picasa.AvaloniaExtensions

open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Media

type LayoutTransformControl with
    static member layoutTransform<'t when 't :> LayoutTransformControl>(value: ITransform) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<ITransform>(LayoutTransformControl.LayoutTransformProperty, value, ValueNone)
