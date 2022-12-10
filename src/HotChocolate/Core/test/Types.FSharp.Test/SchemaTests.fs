module SchemaTests

open System.Text.Json
open HotChocolate
open HotChocolate.Execution
open HotChocolate.Tests
open HotChocolate.Types.FSharp
open Xunit
open Microsoft.Extensions.DependencyInjection
open Swensen.Unquote

type Person =
  { Id: int
    Name: string
    Aliases: string list }

type PersonWithOptionalName =
  { Id: int
    OptionalName: string option }

type Query() =
  member _.GetPerson() =
    { Id = 1; Name = "Michael"; Aliases = [ "Mickey" ] }

  member _.GetPersonWithOptionalName() =
    { Id = 2; OptionalName = Some "Not Michael" }

  member _.GetPersonWithNoName() =
    { Id = 3; OptionalName = None }

[<Fact>]
let ``Schema can be resolved`` () =
  task {
    let! _ =
      ServiceCollection()
        .AddGraphQL()
        .AddQueryType<Query>()
        .AddTypeConverter<OptionTypeConverter>()
        .Services.BuildServiceProvider()
        .GetSchemaAsync()
        .MatchSnapshotAsync()

    // If we got all the way here the schema resolved just fine
    Assert.True(true)
  }

let makeSchema () =
  ServiceCollection()
    .AddGraphQL()
    .AddQueryType<Query>()
    .RegisterFSharpTypeConverters()
    .Services.BuildServiceProvider()
    .GetRequiredService<IRequestExecutorResolver>()
    .GetRequestExecutorAsync()

[<Fact>]
let ``Person can be fetched`` () =
  task {
    let! schema = makeSchema()
    let! result = schema.ExecuteAsync("query {person {id, name, aliases}}")

    let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    let json = JsonSerializer.Deserialize<JsonElement>(result.ToJson(), opts)
    let actual = json.GetProperty("data").GetProperty("person").Deserialize<Person>(opts)
    let expected = { Name = "Michael"; Id = 1; Aliases = ["Mickey"] }

    test <@ actual = expected @>
  }

[<Fact>]
let ``Fetching a person with an optional name works`` () =
  task {
    let! schema = makeSchema()
    let! result = schema.ExecuteAsync("query {personWithOptionalName {id, optionalName}}")

    let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    let json = JsonSerializer.Deserialize<JsonElement>(result.ToJson(), opts)
    let actual = json.GetProperty("data").GetProperty("personWithOptionalName").Deserialize<PersonWithOptionalName>(opts)
    let expected = { OptionalName = Some "Not Michael"; Id = 2 }

    test <@ actual = expected @>
  }


[<Fact>]
let ``Fetching a person with no name works`` () =
  task {
    let! schema = makeSchema()
    let! result = schema.ExecuteAsync("query {personWithNoName {id, optionalName}}")

    let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    let json = JsonSerializer.Deserialize<JsonElement>(result.ToJson(), opts)
    let actual = json.GetProperty("data").GetProperty("personWithNoName").Deserialize(opts)
    let expected = { OptionalName = None; Id = 3 }

    test <@ actual = expected @>
  }
