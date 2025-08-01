﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

namespace FSharp.Compiler.UnitTests

open Xunit
open FSharp.Compiler.Diagnostics
open FSharp.Test

module StructActivePatternTests =

    let private pass = CompilerAssert.PassWithOptions [||]
    let private fail = CompilerAssert.TypeCheckWithErrorsAndOptions [| "--langversion:8.0"|]
    let private run src = CompilerAssert.CompileExeAndRunWithOptions(
        [||],
        ("""
let fail msg =
    printfn "%s" msg
    failwith msg
""" + src))

    [<Fact>]
    let ``Partial active pattern returns Option`1`` () =
        pass "let (|Foo|_|) x = None"

    [<Fact>]
    let ``Partial struct active pattern returns ValueOption`1`` () =
        pass "[<return:Struct>] let (|P1|_|) x = ValueNone"

    [<Fact>]
    let ``StructAttribute can be placed at the active pattern return type annotation`` () =
        pass
            """
let (|P1|_|) x: [<Struct>] _ = ValueNone
let (|P2|_|) x: [<return:Struct>] _ = ValueNone
            """

    [<Fact>]
    let ``Partial struct active pattern results can be retrieved`` () =
        run """
[<Struct>]
type T1 = { v1: int }
and T2 = 
  | T2C1 of int * string
  | T2C2 of T1 * T2
and [<Struct>] T3 = { v3: T2 }
and T4() =
    let mutable _v4 = { v3 = T2C2({v1=0}, T2C1(1, "hey")) }
    member __.v4 with get() = _v4 and set (x) = _v4 <- x

[<return:Struct>] 
let (|P1|_|) =
    function
    | 0 -> ValueNone
    | _ -> ValueSome()

[<return:Struct>] 
let (|P2|_|) =
    function
    | "foo" -> ValueNone
    | _ -> ValueSome "bar"

[<return:Struct>] 
let (|P3|_|) (x: T2) =
  match x with
  | T2C1(a, b) -> ValueSome(a, b)
  | _ -> ValueNone

[<return:Struct>] 
let (|P4|_|) (x: T4) =
  match x.v4 with
  | { v3 = T2C2 ({v1=a}, P3(b, c)) } -> ValueSome (a, b, c)
  | _ -> ValueNone

match 0, 1 with
| P1, _ -> fail "unit"
| _, P1 -> ()
| _     -> fail "unit"

match "foo", "bar" with
| P2 _, _ -> fail "string"
| _, P2("bar") -> ()
| _ -> fail "string"

let t4 = T4()
match t4 with
| P4 (0, 1, "hey") -> ()
| _ -> fail "nested"
            """

    [<Fact>]
    let ``[<return:>] attribute is rotated to the return value``() = 
        run
            """
open System

[<AttributeUsage(AttributeTargets.ReturnValue)>]
type MyAttribute() =
    inherit Attribute()

let extract xs = 
  xs
  |> Seq.map (fun x -> x.GetType().Name)
  |> List.ofSeq
  |> FSharp.Core.String.concat ","

[<return: My>]
let Fn () = ()

let method = Fn.GetType()
               .DeclaringType
               .GetMethod("Fn")

let ret_attrs = method.ReturnTypeCustomAttributes
                      .GetCustomAttributes(false)
                      |> extract

let binding_attrs = method.GetCustomAttributes(false)
                    |> extract

match ret_attrs, binding_attrs with
| "MyAttribute", "" -> ()
| _ -> fail $"ret_attrs = {ret_attrs}, binding_attrs = {binding_attrs} method = {method}"
            """
    [<Fact>]
    let ``Implicitly-targeted attribute on let binding do not target return``() = 
        run
            """
open System

[<AttributeUsage(AttributeTargets.ReturnValue ||| AttributeTargets.Method)>]
type MyAttribute() =
    inherit Attribute()

let extract xs = 
  xs
  |> Seq.map (fun x -> x.GetType().Name)
  |> List.ofSeq
  |> FSharp.Core.String.concat ","

[<My>]
let Fn () = ()

let method = Fn.GetType()
               .DeclaringType
               .GetMethod("Fn")

let ret_attrs = method.ReturnTypeCustomAttributes
                      .GetCustomAttributes(false)
                      |> extract

let binding_attrs = method.GetCustomAttributes(false)
                    |> extract

match ret_attrs, binding_attrs with
| "", "MyAttribute" -> ()
| _ -> fail $"ret_attrs = {ret_attrs}, binding_attrs = {binding_attrs} method = {method}"
            """


// negative tests

    [<Fact>]
    let ``Struct active pattern (-langversion:5.0)`` () =
        CompilerAssert.TypeCheckWithErrorsAndOptions  [| "--langversion:5.0"|]
            """
[<return:Struct>]
let (|Foo|_|) x = ValueNone
            """
            [|(FSharpDiagnosticSeverity.Error, 3350, (3, 6, 3, 13),
               "Feature 'struct representation for active patterns' is not available in F# 5.0. Please use language version 6.0 or greater.")|]

    [<Fact>]
    let ``StructAttribute must explicitly target active pattern return value`` () =
        fail
            """
[<Struct>]
let (|Foo|_|) x = ValueNone
"""
            [|(FSharpDiagnosticSeverity.Warning, 842, (2, 3, 2, 9),
               "This attribute cannot be applied to method, property, field, return value. Valid targets are: class, struct, parameter, return value")
              (FSharpDiagnosticSeverity.Error, 3350, (3, 6, 3, 13),
               "Feature 'Boolean-returning and return-type-directed partial active patterns' is not available in F# 8.0. Please use language version 9.0 or greater.")|]

    [<Fact>]
    let ``StructAttribute not allowed on other bindings than partial active pattern definitions`` () =
        fail 
            """
[<return:Struct>] 
let x = 1

[<return:Struct>]
let f x = x

[<return:Struct>]
let (|A|B|) x = A
"""
            [|(FSharpDiagnosticSeverity.Error, 3385, (3, 5, 3, 6),
               "The use of '[<Struct>]' on values, functions and methods is only allowed on partial active pattern definitions")
              (FSharpDiagnosticSeverity.Error, 3385, (6, 5, 6, 6),
               "The use of '[<Struct>]' on values, functions and methods is only allowed on partial active pattern definitions")
              (FSharpDiagnosticSeverity.Error, 3385, (9, 6, 9, 11),
               "The use of '[<Struct>]' on values, functions and methods is only allowed on partial active pattern definitions")|]

