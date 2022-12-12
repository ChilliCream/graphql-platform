namespace HotChocolate.Types.FSharp

open System
open HotChocolate.Utilities


type OptionTypeConverter() =
  let ty = typedefof<option<_>>

  let isOptionType (t: Type) =
    t.IsGenericType && t.GetGenericTypeDefinition() = ty

  let getUnderlyingType (t: Type) =
    if isOptionType t then
      t.GetGenericArguments() |> Array.tryHead
    else
      None

  // HotChocolate expects us to provide a converter of type `obj -> obj`
  // but we know that we'll receive an `option<_>`. However F# only allows you
  // to define generic type parameters in function declarations. So our only
  // solution is to rely on reflection trickery to unpack the internal obj
  // See https://stackoverflow.com/questions/6289761/how-to-downcast-from-obj-to-optionobj
  let (|SomeObj|_|) (a: obj) =
    if a = null then
      None
    else
      let aty = a.GetType()
      let v = aty.GetProperty("Value")

      if aty.IsGenericType && aty.GetGenericTypeDefinition() = ty then
        if a = null then None else Some(v.GetValue(a, [||]))
      else
        None

  let convertToNullable inner (value: obj) =
    match value with
    | SomeObj value -> inner value
    | _ -> null

  let createTypedSome value =
    ty.MakeGenericType(value.GetType()).GetMethod("Some").Invoke(null, [| value |])

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
