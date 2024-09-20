module OptionConverterTests

open FsCheck.Xunit
open HotChocolate.Types.FSharp
open HotChocolate.Utilities
open Swensen.Unquote

[<Property>]
let ``The option converter can decode all Some options`` (value: obj) =
  let converter = OptionTypeConverter() :> IChangeTypeProvider
  let source = typedefof<option<_>>.MakeGenericType(value.GetType())
  let target = value.GetType()
  match converter.TryCreateConverter(source, target, ChangeTypeProvider(fun source target out -> out <- id; true)) with
  | true, c -> test <@  c.Invoke(Some value) = value @>
  | _ -> failwith "Couldn't create provider"

[<Property>]
let ``The option converter can decode None for all option types`` (value: obj) =
  let converter = OptionTypeConverter() :> IChangeTypeProvider
  let source = typedefof<option<_>>.MakeGenericType(value.GetType())
  let target = value.GetType()
  match converter.TryCreateConverter(source, target, ChangeTypeProvider(fun source target out -> out <- id; true)) with
  | true, c -> test <@  c.Invoke(None) = null @>
  | _ -> failwith "Couldn't create provider"
