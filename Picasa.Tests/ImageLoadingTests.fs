module Picasa.Tests.ImageLoadingTests

open Avalonia
open Picasa
open Xunit
open Swensen.Unquote

[<Fact>]
let ``Loads *.heic`` () =
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .Instance |> ignore
    let file = "/Users/mic/Downloads/IMG_1028.HEIC"
    let img = Images.loadImage (Path file, None)

    test <@ not (isNull img.OriginalImage) @>
