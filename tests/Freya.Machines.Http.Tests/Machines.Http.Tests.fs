﻿module Freya.Machines.Http.Tests

open System
open Arachne.Http
open Arachne.Language
open Freya.Core
open Freya.Core.Operators
open Freya.Machines.Http
open Freya.Optics.Http
open Freya.Testing
open Freya.Testing.Operators
open Xunit

(* Tests

   Behavioural tests of a Freya HTTP machine given different configurations and
   inputs, designed to give test cases aligning to the semantics of the HTTP
   specifications where appropriate. *)

let defaultSetup =
    Freya.empty

let defaultMachine =
    freyaMachine {
        return () }

(* Defaults

   Verification of default behaviour of an unconfigured Freya HTTP machine.
   Only relatively simple behaviour can be expected, but verification of status
   codes, reason phrases, etc. can be verified, along with the correct set of
   responses allowed, etc. *)

module Defaults =

    [<Fact>]
    let ``default machine handles GET request appropriately`` () =

        verify defaultSetup defaultMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

    [<Fact>]
    let ``default machine handles HEAD request appropriately`` () =

        let setup =
            Request.method_ .= HEAD

        verify setup defaultMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

    [<Fact>]
    let ``default machine handles OPTIONS request appropriately`` () =

        let setup =
            Request.method_ .= OPTIONS

        verify setup defaultMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "Options" ]

    [<Fact>]
    let ``default machine handles POST request appropriately`` () =

        let setup =
            Request.method_ .= POST

        verify setup defaultMachine [
            Response.statusCode_ => Some 405
            Response.reasonPhrase_ => Some "Method Not Allowed"
            Response.Headers.allow_ => Some (Allow [ HEAD; GET; OPTIONS ]) ]

(* Assertion

   Verification that the Assertion element behaves as expected given suitable
   input. *)

module Assertion =

    (* Service Available *)

    [<Fact>]
    let ``machine handles serviceAvailable correctly`` () =

        (* Static *)

        let staticMachine =
            freyaMachine {
                serviceAvailable false }

        verify defaultSetup staticMachine [
            Response.statusCode_ => Some 503
            Response.reasonPhrase_ => Some "Service Unavailable" ]

        (* Dynamic *)

        let setup =
            Request.path_ .= "/available"

        let dynamicMachine =
            freyaMachine {
                serviceAvailable ((=) "/available" <!> !. Request.path_) }

        verify setup dynamicMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        verify defaultSetup dynamicMachine [
            Response.statusCode_ => Some 503
            Response.reasonPhrase_ => Some "Service Unavailable" ]

    (* Http Version Supported *)

    [<Fact>]
    let ``machine handles httpVersionSupported correctly`` () =

        (* Static *)

        let staticMachine =
            freyaMachine {
                httpVersionSupported false }

        verify defaultSetup staticMachine [
            Response.statusCode_ => Some 505
            Response.reasonPhrase_ => Some "HTTP Version Not Supported" ]

        (* Default *)

        let supportedSetup =
            Request.httpVersion_ .= HTTP 1.1

        let unsupportedSetup =
            Request.httpVersion_ .= HTTP 1.0

        verify supportedSetup defaultMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        verify unsupportedSetup defaultMachine [
            Response.statusCode_ => Some 505
            Response.reasonPhrase_ => Some "HTTP Version Not Supported" ]

    (* Method Implemented *)

    [<Fact>]
    let ``machine handles methodImplemented correctly`` () =

        (* Inferred *)

        let allowedSetup =
            Request.method_ .= Method.Custom "FOO"

        let notAllowedSetup =
            Request.method_ .= Method.Custom "BAR"

        let machine =
            freyaMachine {
                methods [ Method.Custom "FOO" ] }

        verify allowedSetup machine [
            Response.statusCode_ => Some 200 ]

        verify notAllowedSetup machine [
            Response.statusCode_ => Some 501
            Response.reasonPhrase_ => Some "Not Implemented" ]

        (* Default *)

        let notImplementedSetup =
            Request.method_ .= Method.Custom "FOO"

        verify defaultSetup defaultMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        verify notImplementedSetup defaultMachine [
            Response.statusCode_ => Some 501
            Response.reasonPhrase_ => Some "Not Implemented" ]

(* Permission

   Verification that the Permission element behaves as expected given suitable
   input. *)

module Permission =

    (* Authorized *)

    [<Fact>]
    let ``machine handles authorized correctly`` () =

        (* Static *)

        let staticMachine =
            freyaMachine {
                authorized false }

        verify defaultSetup staticMachine [
            Response.statusCode_ => Some 401
            Response.reasonPhrase_ => Some "Unauthorized" ]

        (* Dynamic *)

        let setup =
            Request.path_ .= "/authorized"

        let dynamicMachine =
            freyaMachine {
                authorized ((=) "/authorized" <!> !. Request.path_) }

        verify setup dynamicMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        verify defaultSetup dynamicMachine [
            Response.statusCode_ => Some 401
            Response.reasonPhrase_ => Some "Unauthorized" ]

    (* Allowed *)

    [<Fact>]
    let ``machine handles allowed correctly`` () =

        (* Static *)

        let staticMachine =
            freyaMachine {
                allowed false }

        verify defaultSetup staticMachine [
            Response.statusCode_ => Some 403
            Response.reasonPhrase_ => Some "Forbidden" ]

        (* Dynamic *)

        let setup =
            Request.path_ .= "/allowed"

        let dynamicMachine =
            freyaMachine {
                allowed ((=) "/allowed" <!> !. Request.path_) }

        verify setup dynamicMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        verify defaultSetup dynamicMachine [
            Response.statusCode_ => Some 403
            Response.reasonPhrase_ => Some "Forbidden" ]

(* Validation

   Verification that the Validation element behaves as expected given suitable
   input. *)

module Validation =

    (* Expectation Met *)

    [<Fact>]
    let ``machine handles expectationMet correctly`` () =

        (* Static *)

        let staticMachine =
            freyaMachine {
                expectationMet false }

        verify defaultSetup staticMachine [
            Response.statusCode_ => Some 417
            Response.reasonPhrase_ => Some "Expectation Failed" ]

        (* Dynamic *)

        let setup =
            Request.path_ .= "/unmet"

        let dynamicMachine =
            freyaMachine {
                expectationMet ((<>) "/unmet" <!> !. Request.path_) }

        verify setup dynamicMachine [
            Response.statusCode_ => Some 417
            Response.reasonPhrase_ => Some "Expectation Failed" ]

        verify defaultSetup dynamicMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

    (* Method Allowed *)

    [<Fact>]
    let ``machine handles method allowance correctly`` () =

        (* Static *)

        let staticMachine =
            freyaMachine {
                methods POST }

        verify defaultSetup staticMachine [
            Response.statusCode_ => Some 405
            Response.reasonPhrase_ => Some "Method Not Allowed"
            Response.Headers.allow_ => Some (Allow [ POST ]) ]

    (* URI Too Long *)

    [<Fact>]
    let ``machine handles uriTooLong correctly`` () =

        (* Static *)

        let staticMachine =
            freyaMachine {
                uriTooLong true }

        verify defaultSetup staticMachine [
            Response.statusCode_ => Some 414
            Response.reasonPhrase_ => Some "URI Too Long" ]

        (* Dynamic *)

        let setup =
            Request.path_ .= "/uritoolong"

        let dynamicMachine =
            freyaMachine {
                uriTooLong ((=) "/uritoolong" <!> !. Request.path_) }

        verify setup dynamicMachine [
            Response.statusCode_ => Some 414
            Response.reasonPhrase_ => Some "URI Too Long" ]

        verify defaultSetup dynamicMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

    (* Bad Request *)

    [<Fact>]
    let ``machine handles badRequest correctly`` () =

        (* Static *)

        let staticMachine =
            freyaMachine {
                badRequest true }

        verify defaultSetup staticMachine [
            Response.statusCode_ => Some 400
            Response.reasonPhrase_ => Some "Bad Request" ]

        (* Dynamic *)

        let setup =
            Request.path_ .= "/badrequest"

        let dynamicMachine =
            freyaMachine {
                badRequest ((=) "/badrequest" <!> !. Request.path_) }

        verify setup dynamicMachine [
            Response.statusCode_ => Some 400
            Response.reasonPhrase_ => Some "Bad Request" ]

        verify defaultSetup dynamicMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

(* Negotiation

   Verification that the Negotiation element behaves as expected given suitable
   input. The neogtiation block may return 412 for any failure to negotiate. *)

module Negotiation =

    (* Accept *)

    [<Fact>]
    let ``machine handles accept negotiation correctly`` () =

        let mediaRange =
            Closed (Type "text", SubType "plain", Parameters Map.empty)

        let acceptParameters =
            AcceptParameters (Weight 1., Extensions Map.empty)

        let accept =
            Accept [ AcceptableMedia (mediaRange, Some acceptParameters) ]

        let setup =
            Request.Headers.accept_ .= Some accept

        (* Unconfigured *)

        verify setup defaultMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        (* Configured *)

        let matchedMachine =
            freyaMachine {
                mediaTypesSupported (MediaType (Type "text", SubType "plain", Parameters Map.empty)) }

        verify setup matchedMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        let unmatchedMachine =
            freyaMachine {
                mediaTypesSupported (MediaType (Type "application", SubType "json", Parameters Map.empty)) }

        verify setup unmatchedMachine [
            Response.statusCode_ => Some 406
            Response.reasonPhrase_ => Some "Not Acceptable" ]

    (* Accept-Language *)

    [<Fact>]
    let ``machine handles accept-language negotiation correctly`` () =

        let acceptLanguage =
            AcceptLanguage [ AcceptableLanguage (Range [ "en" ], None) ]

        let setup =
            Request.Headers.acceptLanguage_ .= Some acceptLanguage

        (* Unconfigured *)

        verify setup defaultMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        (* Configured *)

        let matchedMachine =
            freyaMachine {
                languagesSupported (LanguageTag (Language ("en", None), None, None, Variant [])) }

        verify setup matchedMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        let unmatchedMachine =
            freyaMachine {
                languagesSupported (LanguageTag (Language ("de", None), None, None, Variant [])) }

        verify setup unmatchedMachine [
            Response.statusCode_ => Some 406
            Response.reasonPhrase_ => Some "Not Acceptable" ]

    (* Accept-Charset *)

    [<Fact>]
    let ``machine handles accept-charset negotiation correctly`` () =

        let acceptCharset =
            AcceptCharset [ AcceptableCharset (CharsetRange.Charset (Charset "utf-8"), None) ]

        let setup =
            Request.Headers.acceptCharset_ .= Some acceptCharset

        (* Unconfigured *)

        verify setup defaultMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        (* Configured *)

        let matchedMachine =
            freyaMachine {
                charsetsSupported (Charset "utf-8") }

        verify setup matchedMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        let unmatchedMachine =
            freyaMachine {
                charsetsSupported (Charset "iso-8859-1") }

        verify setup unmatchedMachine [
            Response.statusCode_ => Some 406
            Response.reasonPhrase_ => Some "Not Acceptable" ]

    (* Accept-Encoding *)

    [<Fact>]
    let ``machine handles accept-encoding negotiation correctly`` () =

        let acceptEncoding =
            AcceptEncoding [ AcceptableEncoding (Coding (ContentCoding "gzip"), None) ]

        let setup =
            Request.Headers.acceptEncoding_ .= Some acceptEncoding

        (* Unconfigured *)

        verify setup defaultMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        (* Configured *)

        let matchedMachine =
            freyaMachine {
                contentCodingsSupported (ContentCoding "gzip") }

        verify setup matchedMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        let unmatchedMachine =
            freyaMachine {
                contentCodingsSupported (ContentCoding "compress") }

        verify setup unmatchedMachine [
            Response.statusCode_ => Some 406
            Response.reasonPhrase_ => Some "Not Acceptable" ]

(* Existence

   Verification that the Existence element behaves as expected given suitable
   input. *)

module Existence =

    (* Exists *)

    [<Fact>]
    let ``machine handles exists decision correctly`` () =

        (* Static *)

        let staticMachine =
            freyaMachine {
                exists true }

        verify defaultSetup staticMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        (* Dynamic *)

        let setup =
            Request.path_ .= "/exists"

        let dynamicMachine =
            freyaMachine {
                exists ((=) "/exists" <!> !. Request.path_) }

        verify setup dynamicMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        verify defaultSetup dynamicMachine [
            Response.statusCode_ => Some 404
            Response.reasonPhrase_ => Some "Not Found" ]

(* Preconditions

   Verification that the various preconditions elements behave as expected. *)

module Preconditions =

    (* Common *)

    module Common =

        (* If-Match *)

        [<Fact>]
        let ``machine handles if-match correctly`` () =

            let anySetup =
                Request.Headers.ifMatch_ .= Some (IfMatch (IfMatchChoice.Any))

            let matchedSetup =
                Request.Headers.ifMatch_ .= Some (IfMatch (IfMatchChoice.EntityTags [ Strong "foo" ]))

            let unmatchedSetup =
                Request.Headers.ifMatch_ .= Some (IfMatch (IfMatchChoice.EntityTags [ Strong "bar" ]))

            let machine =
                freyaMachine {
                    entityTag (Strong "foo") }

            verify anySetup defaultMachine [
                Response.statusCode_ => Some 200
                Response.reasonPhrase_ => Some "OK"
                Response.Headers.eTag_ => None ]

            verify anySetup machine [
                Response.statusCode_ => Some 200
                Response.reasonPhrase_ => Some "OK"
                Response.Headers.eTag_ => Some (ETag (Strong "foo")) ]

            verify matchedSetup machine [
                Response.statusCode_ => Some 200
                Response.reasonPhrase_ => Some "OK"
                Response.Headers.eTag_ => Some (ETag (Strong "foo")) ]

            verify unmatchedSetup machine [
                Response.statusCode_ => Some 412
                Response.reasonPhrase_ => Some "Precondition Failed"
                Response.Headers.eTag_ => None ]

        (* If-Unmodified-Since *)

        [<Fact>]
        let ``machine handles if-unmodified-since correctly`` () =

            let baseDate =
                DateTime (2000, 1, 1)

            let newSetup =
                Request.Headers.ifUnmodifiedSince_ .= Some (IfUnmodifiedSince (baseDate))

            let oldSetup =
                Request.Headers.ifUnmodifiedSince_ .= Some (IfUnmodifiedSince (baseDate.AddDays (-2.)))

            let machine =
                freyaMachine {
                    lastModified (baseDate.AddDays (-1.)) }

            verify newSetup machine [
                Response.statusCode_ => Some 200
                Response.reasonPhrase_ => Some "OK"
                Response.Headers.lastModified_ => Some (LastModified (baseDate.AddDays (-1.))) ]

            verify oldSetup machine [
                Response.statusCode_ => Some 412
                Response.reasonPhrase_ => Some "Precondition Failed" ]

    (* Safe *)

    module Safe =

        (* If-None-Match *)

        [<Fact>]
        let ``machine handles if-none-match correctly`` () =

            let anySetup =
                Request.Headers.ifNoneMatch_ .= Some (IfNoneMatch (IfNoneMatchChoice.Any))

            let matchedSetup =
                Request.Headers.ifNoneMatch_ .= Some (IfNoneMatch (IfNoneMatchChoice.EntityTags [ Weak "foo" ]))

            let unmatchedSetup =
                Request.Headers.ifNoneMatch_ .= Some (IfNoneMatch (IfNoneMatchChoice.EntityTags [ Weak "bar" ]))

            let machine =
                freyaMachine {
                    entityTag (Strong "foo") }

            verify anySetup defaultMachine [
                Response.statusCode_ => Some 200
                Response.reasonPhrase_ => Some "OK"
                Response.Headers.eTag_ => None ]

            verify anySetup machine [
                Response.statusCode_ => Some 200
                Response.reasonPhrase_ => Some "OK"
                Response.Headers.eTag_ => Some (ETag (Strong "foo")) ]

            verify matchedSetup machine [
                Response.statusCode_ => Some 304
                Response.reasonPhrase_ => Some "Not Modified"
                Response.Headers.eTag_ => None ]

            verify unmatchedSetup machine [
                Response.statusCode_ => Some 200
                Response.reasonPhrase_ => Some "OK"
                Response.Headers.eTag_ => Some (ETag (Strong "foo")) ]

        (* If-Modified-Since *)

        [<Fact>]
        let ``machine handles if-modified-since correctly`` () =

            let baseDate =
                DateTime (2000, 1, 1)

            let newSetup =
                Request.Headers.ifModifiedSince_ .= Some (IfModifiedSince (baseDate))

            let oldSetup =
                Request.Headers.ifModifiedSince_ .= Some (IfModifiedSince (baseDate.AddDays (-2.)))

            let machine =
                freyaMachine {
                    lastModified (baseDate.AddDays (-1.)) }

            verify newSetup machine [
                Response.statusCode_ => Some 304
                Response.reasonPhrase_ => Some "Not Modified" ]

            verify oldSetup machine [
                Response.statusCode_ => Some 200
                Response.reasonPhrase_ => Some "OK"
                Response.Headers.lastModified_ => Some (LastModified (baseDate.AddDays (-1.))) ]

    (* Unsafe *)

    module Unsafe =

        (* If-None-Match *)

        [<Fact>]
        let ``machine handles if-none-match correctly`` () =

            let anySetup =
                    (Request.method_ .= POST)
                 *> (Request.Headers.ifNoneMatch_ .= Some (IfNoneMatch (IfNoneMatchChoice.Any)))

            let matchedSetup =
                    (Request.method_ .= POST)
                 *> (Request.Headers.ifNoneMatch_ .= Some (IfNoneMatch (EntityTags [ Weak "foo" ])))

            let unmatchedSetup =
                    (Request.method_ .= POST)
                 *> (Request.Headers.ifNoneMatch_ .= Some (IfNoneMatch (EntityTags [ Weak "bar" ])))

            let machine =
                freyaMachine {
                    methods POST
                    entityTag (Strong "foo") }

            verify anySetup machine [
                Response.statusCode_ => Some 200
                Response.reasonPhrase_ => Some "OK"
                Response.Headers.eTag_ => Some (ETag (Strong "foo")) ]

            verify matchedSetup machine [
                Response.statusCode_ => Some 412
                Response.reasonPhrase_ => Some "Precondition Failed" ]

            verify unmatchedSetup machine [
                Response.statusCode_ => Some 200
                Response.reasonPhrase_ => Some "OK"
                Response.Headers.eTag_ => Some (ETag (Strong "foo")) ]

(* Conflict

   Verification that the Conflict element behaves as expected given suitable
   input. *)

module Conflict =

    (* Conflict *)

    [<Fact>]
    let ``machine handles conflict correctly`` () =

        let nonConflictSetup =
                (Request.method_ .= POST)

        let conflictSetup =
                (Request.path_ .= "/conflict")
             *> (Request.method_ .= POST)

        (* Static *)

        let staticMachine =
            freyaMachine {
                conflict true
                methods POST }

        verify nonConflictSetup staticMachine [
            Response.statusCode_ => Some 409
            Response.reasonPhrase_ => Some "Conflict" ]

        (* Dynamic *)

        let dynamicMachine =
            freyaMachine {
                conflict ((=) "/conflict" <!> !. Request.path_)
                methods POST }

        verify nonConflictSetup dynamicMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        verify conflictSetup dynamicMachine [
            Response.statusCode_ => Some 409
            Response.reasonPhrase_ => Some "Conflict" ]

(* Operation

   Verification that the Operation element behaves as expected given suitable
   input, including verification that the operation to be run is executed and
   that the result of the operation influences the response path. *)

module Operation =

    (* Operation *)

    [<Fact>]
    let ``machine processes operation correctly`` () =

        let test_ =
            State.value_<string> "test"

        let setup =
            Request.method_ .= POST

        (* Success *)

        let success =
            freya {
                do! Freya.Optic.set test_ (Some "test") }

        let successMachine =
            freyaMachine {
                doPost success
                methods POST }

        verify setup successMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK"
            test_ => Some "test" ]

        (* Failure *)

        let failure =
            freya {
                do! Freya.Optic.set test_ (Some "test")
                return false }

        let failureMachine =
            freyaMachine {
                doPost failure
                methods POST }

        verify setup failureMachine [
            Response.statusCode_ => Some 500
            Response.reasonPhrase_ => Some "Internal Server Error"
            test_ => Some "test" ]

    (* Completed *)

    [<Fact>]
    let ``machine handles completed correctly`` () =

        let setup =
            Request.method_ .= POST

        (* Completed *)

        let completedMachine =
            freyaMachine {
                methods POST }

        verify setup completedMachine [
            Response.statusCode_ => Some 200
            Response.reasonPhrase_ => Some "OK" ]

        (* Uncompleted *)

        let uncompletedMachine =
            freyaMachine {
                completed false
                methods POST }

        verify setup uncompletedMachine [
            Response.statusCode_ => Some 202
            Response.reasonPhrase_ => Some "Accepted" ]
