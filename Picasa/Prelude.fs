[<AutoOpen>]
module Picasa.Prelude

let (^) f x = f x

type Deferred<'t> =
    | HasNotStartedYet
    | InProgress
    | Resolved of 't
    
type DeferredResult<'t> = Deferred<Result<'t, string>>

type AsyncOperationStatus<'t> =
    | Started
    | Finished of 't

let runAsynchronously f arg = async {
    do! Async.SwitchToThreadPool ()
    try
        return Ok ^ f arg
    with
    | e ->
        // todo log exception
        return Error e.Message
}

type Path = Path of string with
    member this.Value = let (Path path) = this in path