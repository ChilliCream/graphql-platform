namespace HotChocolate.Types.FSharp

open System
open System.Collections.Concurrent
open System.Collections.Generic
open HotChocolate.Utilities
open Microsoft.FSharp.Reflection


[<AutoOpen>]
module private Helpers =

    let private memoizeRefEq (f: 'a -> 'b) =
        let equalityComparer =
            { new IEqualityComparer<'a> with
                member _.Equals(a, b) = LanguagePrimitives.PhysicalEquality a b
                member _.GetHashCode(a) = LanguagePrimitives.PhysicalHash a
            }

        let cache = new ConcurrentDictionary<'a, 'b>(equalityComparer)
        fun a -> cache.GetOrAdd(a, f)

    let private getCachedSomeReader =
        memoizeRefEq (fun ty ->
            let cases = FSharpType.GetUnionCases ty
            let someCase = cases |> Array.find (fun ci -> ci.Name = "Some")
            let read = FSharpValue.PreComputeUnionReader someCase
            fun x -> read x |> Array.head
        )

    let private getCachedSomeConstructor =
        memoizeRefEq (fun innerType ->
            let optionType = typedefof<_ option>.MakeGenericType([| innerType |])
            let cases = FSharpType.GetUnionCases optionType
            let someCase = cases |> Array.find (fun ci -> ci.Name = "Some")
            let create = FSharpValue.PreComputeUnionConstructor(someCase)
            fun x -> create [| x |]
        )

    let fastGetInnerOptionValueAssumingSome (optionValue: obj) : obj =
        getCachedSomeReader (optionValue.GetType()) optionValue

    let fastCreateSome (innerValue: obj) : obj =
        getCachedSomeConstructor (innerValue.GetType()) innerValue

    let fastGetInnerOptionType =
        memoizeRefEq (fun (ty: Type) ->
            if ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<_ option> then
                Some(ty.GetGenericArguments()[0])
            else
                None
        )


type OptionTypeConverter() =

    let mapInner (convertInner: obj -> obj) (optionValue: obj) =
        if isNull optionValue then
            null
        else
            optionValue
            |> fastGetInnerOptionValueAssumingSome
            |> convertInner
            |> fastCreateSome

    let optionToObj (convertInner: obj -> obj) (optionValue: obj) =
        if isNull optionValue then
            null
        else
            optionValue |> fastGetInnerOptionValueAssumingSome |> convertInner

    let objToOption (convertInner: obj -> obj) (value: obj) =
        if isNull value then
            null
        else
            value |> convertInner |> fastCreateSome

    interface IChangeTypeProvider with

        member this.TryCreateConverter
            (
                source: Type,
                target: Type,
                root: ChangeTypeProvider,
                converter: byref<ChangeType>
            ) =
            match fastGetInnerOptionType source, fastGetInnerOptionType target with
            | Some source, Some target ->
                match root.Invoke(source, target) with
                | true, innerConverter ->
                    converter <- ChangeType(mapInner innerConverter.Invoke)
                    true
                | false, _ -> false
            | Some source, None ->
                match root.Invoke(source, target) with
                | true, innerConverter ->
                    converter <- ChangeType(optionToObj innerConverter.Invoke)
                    true
                | _ -> false
            | None, Some target ->
                match root.Invoke(source, target) with
                | true, innerConverter ->
                    converter <- ChangeType(objToOption innerConverter.Invoke)
                    true
                | _ -> false
            | _ -> false
