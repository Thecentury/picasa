module Picasa.Caching

open System
open System.Collections.Generic

open NLog
open Picasa

type Box<'a> = {
    Boxed : 'a
}

type CachedValue<'a> =
    | Ref of 'a
    | WeakRef of WeakReference<Box<'a>>
    
type CacheRecord<'a>(value : 'a) =
    let mutable ref = Ref value
    let mutable lastAccessTime = DateTime.UtcNow
    
    member this.LastAccessTime = lastAccessTime
    
    member this.Value =
        match ref with
        | Ref v ->
            lastAccessTime <- DateTime.UtcNow
            Some v
        | WeakRef wr ->
            let empty, value = wr.TryGetTarget ()
            match empty, box value with
            | true, _
            | _, null ->
                LogManager.GetCurrentClassLogger().Debug "WeakReference is empty"
                None
            | _ ->
                lastAccessTime <- DateTime.UtcNow
                Some value.Boxed
    member this.SetValue (v : 'a) =
        lastAccessTime <- DateTime.UtcNow
        ref <- Ref v
        
    member this.Weaken () =
        match ref with
        | Ref v -> ref <- WeakRef (WeakReference<_>({ Boxed = v }))
        | _ -> ()
        
type IDeletionPolicy =
    abstract Process<'a> : ICollection<CacheRecord<'a>> -> unit
    
let notMoreThanDeletionPolicy (max : int) =
    { new IDeletionPolicy with
        member _.Process c =
            c
            |> Seq.sortByDescending (fun x -> x.LastAccessTime)
            |> Seq.skipSafe max
            |> Seq.iter (fun x -> x.Weaken ())
            () }
    
type Cache<'k, 'v when 'k : comparison> (deletionPolicy : IDeletionPolicy) =
    let mutable map : Map<'k, CacheRecord<'v>> = Map.empty
    
    member this.TryFind key =
        let record = map.TryFind key

        deletionPolicy.Process map.Values

        record |> Option.bind (fun r -> r.Value)

    member this.Add key value =
        match map.TryFind key with
        | None ->
            map <- map.Add (key, CacheRecord value)
        | Some v -> v.SetValue value