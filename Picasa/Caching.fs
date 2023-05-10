module Picasa.Caching

open System
open System.Collections.Generic
open FSharp.Core.Fluent

open NLog
open Picasa

type Box<'a> = {
    Boxed : 'a
}

type CachedValue<'a> =
    | StrongRef of 'a
    | WeakRef of WeakReference<Box<'a>>
    
type CacheRecord<'a, 'weak>(value : 'a, weakValue : 'weak) =
    let mutable ref = StrongRef weakValue
    let mutable lastAccessTime = DateTime.UtcNow
    
    member this.LastAccessTime = lastAccessTime
    member this.Value = value
    member this.WeakValue =
        match ref with
        | StrongRef v ->
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
    member this.SetValue (v : 'weak) =
        lastAccessTime <- DateTime.UtcNow
        ref <- StrongRef v
        
    member this.Weaken () =
        match ref with
        | StrongRef v -> ref <- WeakRef (WeakReference<_>({ Boxed = v }))
        | _ -> ()

#nowarn "3536"
type IDeletionPolicy =
    abstract Process<'a, 'weak> : ICollection<CacheRecord<'a, 'weak>> -> unit
    
let notMoreThanDeletionPolicy (max : int) =
    { new IDeletionPolicy with
        member _.Process c =
            c
            |> Seq.sortByDescending (fun x -> x.LastAccessTime)
            |> Seq.skipSafe max
            |> Seq.iter (fun x -> x.Weaken ())
            () }
    
type Cache<'k, 'v, 'vweak when 'k : comparison> (deletionPolicy : IDeletionPolicy) =
    let mutable map : Map<'k, CacheRecord<'v, 'vweak>> = Map.empty
    
    member this.TryFind key =
        let record = map.TryFind key

        deletionPolicy.Process map.Values

        (record |> Option.bind (fun r -> r.WeakValue), record.map(fun x -> x.Value))

    member this.Add key value weakValue =
        match map.TryFind key with
        | None ->
            map <- map.Add (key, CacheRecord (value, weakValue))
        | Some v -> v.SetValue weakValue
