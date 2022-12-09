module Tests

open System.Text.Json
open HotChocolate
open HotChocolate.Execution
open HotChocolate.Tests
open Xunit
open Microsoft.Extensions.DependencyInjection

type Person =
  { Id: int
    Name: string }

type PersonWithOptionalName =
  { Id: int
    OptionalName: string option }

type Query() =
  member _.GetPerson() =
    { Id = 1; Name = "Michael" }

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
        .Services.BuildServiceProvider()
        .GetSchemaAsync()
        .MatchSnapshotAsync()

    // If we got all the way here the schema resolved just fine
    Assert.True(true)
  }

[<Fact>]
let ``Person can be fetched`` () =
  task {
    let! schema =
      ServiceCollection()
        .AddGraphQL()
        .AddQueryType<Query>()
        .Services.BuildServiceProvider()
        .GetSchemaAsync()

    let! result = schema.MakeExecutable().ExecuteAsync("query {person {id, name}}")

    let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    let json = JsonSerializer.Deserialize<JsonElement>(result.ToJson(), opts)
    let actual = json.GetProperty("data").GetProperty("person").Deserialize<Person>(opts)
    let expected = { Name = "Michael"; Id = 1 }

    Assert.True((actual = expected), "The person was not returned correctly")
  }

[<Fact>]
let ``Fetching a person with an optional name works`` () =
  task {
    let! schema =
      ServiceCollection()
        .AddGraphQL()
        .AddQueryType<Query>()
        .Services.BuildServiceProvider()
        .GetSchemaAsync()

    let! result =
      schema
        .MakeExecutable()
        .ExecuteAsync("query {personWithOptionalName {id, optionalName}}")

    let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    let json = JsonSerializer.Deserialize<JsonElement>(result.ToJson(), opts)
    let actual = json.GetProperty("data").GetProperty("personWithOptionalName").Deserialize<PersonWithOptionalName>(opts)

    let expected =
      { OptionalName = Some "Not Michael"
        Id = 1 }

    Assert.True(
      (expected = actual),
      "The person was not returned correctly"
    )
  }


[<Fact>]
let ``Fetching a person with no name works`` () =
  task {
    let! schema =
      ServiceCollection()
        .AddGraphQL()
        .AddQueryType<Query>()
        .Services.BuildServiceProvider()
        .GetSchemaAsync()

    let! result = schema.MakeExecutable().ExecuteAsync("query {personWithNoName {id, optionalName}}")

    let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    let json = JsonSerializer.Deserialize<JsonElement>(result.ToJson(), opts)
    let actual = json.GetProperty("data").GetProperty("personWithNoName").Deserialize(opts)
    let expected = { OptionalName = None; Id = 1 }

    Assert.True(
      (expected = actual),
      "The person was not returned correctly"
    )
  }
