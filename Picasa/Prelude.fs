[<AutoOpen>]
module Picasa.Prelude

open Avalonia.Media.Imaging
open NLog

let (^) f x = f x

type Deferred<'t> =
    | HasNotStartedYet
    | InProgress
    | Resolved of 't
    
type DeferredResult<'t> = Deferred<Result<'t, string>>

let (|ResolvedOk|_|) = function
    | Resolved (Ok v) -> Some v
    | _ -> None

type AsyncOperationStatus<'t> =
    | Started
    | Finished of 't

let runAsynchronously f arg = async {
    do! Async.SwitchToThreadPool ()
    try
        return Ok ^ f arg
    with
    | e ->
        let logger = LogManager.GetCurrentClassLogger ()
        logger.Error (e, $"Failed to execute runAsynchronously(%A{f}, %A{arg})")
        return Error e.Message
}

[<Struct>]
type Path = Path of string with
    member this.Value = let (Path path) = this in path
    
[<Struct>]
type Rotation =
    | NoRotation
    | Right90
    | Right180
    | Right270
    
[<Struct>]
type RotationDirection = Left | Right
    
module Rotation =
    
    let rotateRight = function
        | NoRotation -> Right90
        | Right90 -> Right180
        | Right180 -> Right270
        | Right270 -> NoRotation

    let rotateLeft = function
        | NoRotation -> Right270
        | Right90 -> NoRotation
        | Right180 -> Right90
        | Right270 -> Right180
        
    let rotate rotation dir =
        match dir with
        | Left -> rotateLeft rotation
        | Right -> rotateRight rotation
        
    let toAngle = function
        | NoRotation -> 0
        | Right90 -> 90
        | Right180 -> 180
        | Right270 -> 270

type RotatedImage = {
    OriginalImage : IBitmap
    RotatedImage : IBitmap
    Rotation : Rotation
}
