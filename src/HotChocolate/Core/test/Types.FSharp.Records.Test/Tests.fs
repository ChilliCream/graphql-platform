module Tests

open System.Text.Json
open HotChocolate
open HotChocolate.Execution
open Xunit
open System
open Microsoft.Extensions.DependencyInjection
open System.Threading.Tasks
open Xunit

type Person =
  { [<GraphQLType "ID">]
    Id: int
    Name: string }

type PersonWithOptionalName =
  { [<GraphQLType "ID">]
    Id: int
    OptionalName: string option }

type Query() =
  member _.GetPerson() = { Id = 1; Name = "Michael" }

[<Fact>]
let ``Schema can be resolved`` () =
  task {
    let! _ =
      ServiceCollection()
        .AddGraphQL()
        .AddQueryType<Query>()
        .Services
        .BuildServiceProvider()
        .GetSchemaAsync()

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
        .Services
        .BuildServiceProvider()
        .GetSchemaAsync()

    let! result =
      schema.MakeExecutable().ExecuteAsync(
        "query {person {id, name}}"
        )

    let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    let actual = JsonSerializer.Deserialize<Person>(result.ToJson(), opts)
    let expected = { Name = "Michael"; Id = 1 }

    Assert.True((expected = actual), "The person was not returned correctly")
  }
