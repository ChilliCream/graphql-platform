namespace HotChocolate.Types.FSharp

open System
open HotChocolate.Utilities
open Microsoft.FSharp.Reflection

type OptionTypeConverter() =
  let optionTypedef = typedefof<option<_>>

  let isOptionType (t: Type) =
    t.IsGenericType && t.GetGenericTypeDefinition() = optionTypedef

  let getUnderlyingType (t: Type) =
    if isOptionType t then
      t.GetGenericArguments() |> Array.tryHead
    else
      None

  let (|SomeObj|_|) (value: obj) =
    value
    |> Option.ofObj
    |> Option.map (fun x -> x.GetType())
    |> Option.filter isOptionType
    |> Option.map (fun x -> FSharpValue.GetUnionFields(value, x))
    |> Option.filter (fun (case, _) -> case.Name = "Some")
    |> Option.bind (fun (_, xs) -> Array.tryHead xs)



  let convertToNullable inner (value: obj) =
    match value with
    | SomeObj value -> inner value
    | _ -> null

  let createTypedSome value =
    let optionalType = optionTypedef.MakeGenericType(value.GetType())
    let case = FSharpType.GetUnionCases optionalType |> Array.find (fun x -> x.Name = "Some")
    FSharpValue.MakeUnion(case, [| value |])

  let mapInner inner (value: obj) =
    match value with
    | SomeObj value -> createTypedSome (inner value) |> box
    | _ -> box None

  let convertToOption inner (value: obj) =
    match value with
    | null -> box None
    | value -> createTypedSome (inner value)

  interface IChangeTypeProvider with

    member this.TryCreateConverter(source: Type, target: Type, root: ChangeTypeProvider, converter: byref<ChangeType>) =
      let innerSource = getUnderlyingType source
      let innerTarget = getUnderlyingType target

      match innerSource, innerTarget with
      | Some source, Some target ->
        match root.Invoke(source, target) with
        | true, innerConverter ->
          converter <- ChangeType(mapInner innerConverter.Invoke)
          true
        | false, _ -> false
      | Some source, None ->
        match root.Invoke(source, target) with
        | true, innerConverter ->
          converter <- ChangeType(convertToNullable innerConverter.Invoke)
          true
        | _ -> false
      | None, Some target ->
        match root.Invoke(source, target) with
        | true, innerConverter ->
          converter <- ChangeType(convertToOption innerConverter.Invoke)
          true
        | _ -> false
      | _ -> false
