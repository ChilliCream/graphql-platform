using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class InterfaceObjectTests
{
    [Fact]
    public static async Task Programme_Is_InterfaceObject()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddType<Programme>()
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task Programme_Is_InterfaceObject_Fluent()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddObjectType(
                    d =>
                    {
                        d.Name("Programme");
                        d.InterfaceObject();
                        d.EntityKey("id");
                        d.Field("id").Type<NonNullType<IdType>>().Resolve("1");
                    })
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task Programme_Is_InterfaceObject_Fluent_Generic()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddType<GenericProgrammeType>()
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task InterfaceObject_With_Explicit_Name_Renames_Type()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddType<RenamedProgramme>()
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        schema.MatchSnapshot();
    }

    public class Query
    {
        public string GetHello() => "World";
    }

    [InterfaceObject]
    [EntityKey("id")]
    public class Programme
    {
        public string Id { get; set; } = default!;
    }

    [InterfaceObject("Programme")]
    [EntityKey("id")]
    public class RenamedProgramme
    {
        public string Id { get; set; } = default!;
    }

    public class GenericProgramme
    {
        public string Id { get; set; } = default!;
    }

    // fluent authoring of the @interfaceObject and @key directives through IObjectTypeDescriptor<T>,
    // without casting to the non-generic IObjectTypeDescriptor.
    public class GenericProgrammeType : ObjectType<GenericProgramme>
    {
        protected override void Configure(IObjectTypeDescriptor<GenericProgramme> descriptor)
        {
            descriptor.Name("GenericProgramme");
            descriptor.InterfaceObject();
            descriptor.EntityKey("id");
        }
    }
}
