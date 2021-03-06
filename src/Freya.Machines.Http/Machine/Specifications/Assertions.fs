﻿namespace Freya.Machines.Http.Machine.Specifications

open Aether
open Aether.Operators
open Freya.Core.Operators
open Freya.Machines
open Freya.Machines.Http
open Freya.Machines.Http.Machine.Configuration
open Freya.Optics.Http
open Freya.Types.Http

(* Assertions

   Decisions asserting the capability of the server to handle the given
   request, based on the defined availability, HTTP protocols, etc.

   Failures of these assertions/checks of server capability will result
   in 5xx error responses, signalling a server error of some type. *)

[<RequireQualifiedAccess>]
module Assertions =

    (* Types *)

    type private Assertions =
        { Decisions: Decisions
          Terminals: Terminals }

        static member decisions_ =
            (fun x -> x.Decisions), (fun d x -> { x with Decisions = d })

        static member terminals_ =
            (fun x -> x.Terminals), (fun t x -> { x with Terminals = t })

        static member empty =
            { Decisions = Decisions.empty
              Terminals = Terminals.empty }

     and private Decisions =
        { ServiceAvailable: Value<bool> option
          HttpVersionSupported: Value<bool> option }

        static member serviceAvailable_ =
            (fun x -> x.ServiceAvailable), (fun s x -> { x with ServiceAvailable = s })

        static member httpVersionSupported_ =
            (fun x -> x.HttpVersionSupported), (fun h x -> { x with HttpVersionSupported = h })

        static member empty =
            { ServiceAvailable = None
              HttpVersionSupported = None }

     and private Terminals =
        { ServiceUnavailable: Handler option
          HttpVersionNotSupported: Handler option
          NotImplemented: Handler option }

        static member serviceUnavailable_ =
            (fun x -> x.ServiceUnavailable), (fun s x -> { x with ServiceUnavailable = s })

        static member httpVersionNotSupported_ =
            (fun x -> x.HttpVersionNotSupported), (fun h x -> { x with HttpVersionNotSupported = h })

        static member notImplemented_ =
            (fun x -> x.NotImplemented), (fun n x -> { x with NotImplemented = n })

        static member empty =
            { ServiceUnavailable = None
              HttpVersionNotSupported = None
              NotImplemented = None }

    (* Key *)

    let private key =
        Key.root >> Key.add [ "assertions" ]

    (* Optics *)

    let private assertions_ =
        Configuration.element_ Assertions.empty [ "http"; "specifications"; "assertions" ]

    (* Terminals *)

    [<RequireQualifiedAccess>]
    [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
    module Terminals =

        let private terminals_ =
                assertions_
            >-> Assertions.terminals_

        let serviceUnavailable_ =
                terminals_
            >-> Terminals.serviceUnavailable_

        let httpVersionNotSupported_ =
                terminals_
            >-> Terminals.httpVersionNotSupported_

        let notImplemented_ =
                terminals_
            >-> Terminals.notImplemented_

        let serviceUnavailable k =
            Terminal.create (key k, "handleServiceUnavailable")
                (function | _ -> Operations.serviceUnavailable)
                (function | Get serviceUnavailable_ x -> x) 

        let httpVersionNotSupported k =
            Terminal.create (key k, "handleHttpVersionNotSupported")
                (function | _ -> Operations.httpVersionNotSupported)
                (function | Get httpVersionNotSupported_ x -> x)

        let notImplemented k =
            Terminal.create (key k, "handleNotImplemented")
                (function | _ -> Operations.notImplemented)
                (function | Get notImplemented_ x -> x)

    (* Decisions *)

    [<RequireQualifiedAccess>]
    [<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
    module Decisions =

        let private decisions_ =
                assertions_
            >-> Assertions.decisions_

        let serviceAvailable_ =
                decisions_
            >-> Decisions.serviceAvailable_

        let httpVersionSupported_ =
                decisions_
            >-> Decisions.httpVersionSupported_

        let rec serviceAvailable k s =
            Decision.create (key k, "serviceAvailable")
                (function | TryGetOrElse serviceAvailable_ (Static true) x -> x)
                (Terminals.serviceUnavailable k, httpVersionSupported k s)

        and httpVersionSupported k s =
            Decision.create (key k, "httpVersionSupported")
                (function | TryGet httpVersionSupported_ x -> x
                          | _ -> Dynamic supported)
                (Terminals.httpVersionNotSupported k, methodImplemented k s)

        and private supported =
                function | HTTP x when x >= 1.1 -> true
                         | _ -> false
            <!> !. Request.httpVersion_

        and methodImplemented k s =
            Decision.create (key k, "methodImplemented")
                (function | TryGet Properties.Request.methods_ x -> Value.Freya.bind knownCustom x
                          | _ -> Dynamic nonCustom)
                (Terminals.notImplemented k, s)

        and private knownCustom methodsAllowed =
                function | Method.Custom x when not (Set.contains (Method.Custom x) methodsAllowed) -> false
                         | _ -> true
            <!> !. Request.method_

        and private nonCustom =
                function | Method.Custom _ -> false
                         | _ -> true
            <!> !. Request.method_

    (* Specification *)

    let specification =
        Decisions.serviceAvailable