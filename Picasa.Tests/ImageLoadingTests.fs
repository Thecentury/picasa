module Picasa.Tests.ImageLoadingTests

open Picasa
open Xunit
open Swensen.Unquote

[<Fact>]
let ``Loads *.heic`` () =
    let file = "/Users/mic/Downloads/IMG_6881.HEIC"
    let img = Images.loadImage (Path file, None)

    test <@ not (isNull img.OriginalImage) @>
