﻿open System

// Freya

open Arachne.Http
open Freya.Core
open Freya.Machines.Http

let machine =
    freyaMachine {
        mediaTypesSupported MediaType.Json }

let app =
    OwinAppFunc.ofFreya machine

// Katana

open Microsoft.Owin.Hosting

type App () =
    member __.Configuration () =
        app

let server () =
    WebApp.Start<App> "http://localhost:7000"

// Main

[<EntryPoint>]
let main _ =

    let _ = server ()
    let _ = Console.ReadLine ()

    0
