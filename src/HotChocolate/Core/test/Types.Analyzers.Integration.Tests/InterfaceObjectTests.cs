using HotChocolate.Execution;
using HotChocolate.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class InterfaceObjectTests
{
    [Fact]
    public async Task Generic_Attribute_StandIn_Is_Printed_With_InterfaceObject_And_Key_Directives()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddSourceSchemaDefaults()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var sdl = SchemaFormatter.FormatAsString(schema);

        // assert
        Assert.Contains("type Programme @interfaceObject @key(fields: \"id\") {", sdl);
        Assert.Contains(
            "programmeById(id: String! @is(field: \"id\")): Programme @lookup @internal",
            sdl);
    }

    [Fact]
    public async Task
        NonGeneric_Attribute_StandIn_Registered_Through_Generated_Module_Is_Printed_With_InterfaceObject_Directive()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddSourceSchemaDefaults()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var sdl = SchemaFormatter.FormatAsString(schema);

        // assert
        // Programme2 carries no [ObjectType]; it reaches the schema solely through the
        // generated module's builder.AddType<Programme2>() auto-registration.
        Assert.Contains("type Programme2 @interfaceObject @key(fields: \"id\") {", sdl);
    }

    [Fact]
    public async Task Lookup_Resolves_Field_Contributed_By_Generic_StandIn()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddSourceSchemaDefaults()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ programmeById(id: \"1\") { allowedUserActions } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "programmeById": {
                  "allowedUserActions": [
                    "view",
                    "edit"
                  ]
                }
              }
            }
            """);
    }
}
