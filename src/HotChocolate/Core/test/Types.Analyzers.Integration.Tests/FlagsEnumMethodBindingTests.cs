using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class FlagsEnumMethodBindingTests
{
    [Fact]
    public async Task FlagsEnum_Should_CoerceToFlagsObject_When_ReturnedFromPropertyBoundField()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .ModifyOptions(o => o.EnableFlagEnums = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ animal { kind { isDog isCat } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "animal": {
                  "kind": {
                    "isDog": true,
                    "isCat": true
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task FlagsEnum_Should_CoerceToFlagsObject_When_ReturnedFromMethodBoundField()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .ModifyOptions(o => o.EnableFlagEnums = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ zoo { animals { isDog isCat } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "zoo": {
                  "animals": {
                    "isDog": true,
                    "isCat": true
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task FlagsEnum_Should_PreserveWrappers_When_ReturnedFromMethodBoundFields()
    {
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .ModifyOptions(o => o.EnableFlagEnums = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
            """
            {
              zoo {
                propertyAnimals {
                  isDog
                  isCat
                }
                nullableAnimals {
                  isDog
                  isCat
                }
                taskAnimals {
                  isDog
                  isCat
                }
                valueTaskAnimals {
                  isDog
                  isCat
                }
                animalArray {
                  isDog
                  isCat
                }
                animalList {
                  isDog
                  isCat
                }
                nestedAnimalList {
                  isDog
                  isCat
                }
                taskAnimalList {
                  isDog
                  isCat
                }
                batchAnimals {
                  isDog
                  isCat
                }
                fauxFlags
              }
            }
            """,
            TestContext.Current.CancellationToken);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "zoo": {
                  "propertyAnimals": {
                    "isDog": true,
                    "isCat": true
                  },
                  "nullableAnimals": null,
                  "taskAnimals": {
                    "isDog": true,
                    "isCat": true
                  },
                  "valueTaskAnimals": {
                    "isDog": true,
                    "isCat": true
                  },
                  "animalArray": [
                    {
                      "isDog": true,
                      "isCat": true
                    },
                    null
                  ],
                  "animalList": [
                    {
                      "isDog": true,
                      "isCat": true
                    }
                  ],
                  "nestedAnimalList": [
                    [
                      {
                        "isDog": true,
                        "isCat": true
                      }
                    ]
                  ],
                  "taskAnimalList": [
                    {
                      "isDog": true,
                      "isCat": true
                    }
                  ],
                  "batchAnimals": {
                    "isDog": true,
                    "isCat": true
                  },
                  "fauxFlags": "FIRST"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task FlagsEnum_Should_CoerceInput_When_UsedByGeneratedResolverMethods()
    {
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .ModifyOptions(o => o.EnableFlagEnums = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
            """
            {
              formatAnimalKinds(kinds: { isDog: true, isCat: true })
              formatNullableAnimalKinds
              formatAnimalKindList(
                kinds: [
                  { isDog: true, isCat: false }
                  null
                  { isDog: false, isCat: true }
                ])
            }
            """,
            TestContext.Current.CancellationToken);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "formatAnimalKinds": "Dog, Cat",
                "formatNullableAnimalKinds": "null",
                "formatAnimalKindList": "Dog,null,Cat"
              }
            }
            """);
    }
}
