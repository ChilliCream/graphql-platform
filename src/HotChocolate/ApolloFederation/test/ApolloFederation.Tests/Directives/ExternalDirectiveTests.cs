using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.Directives;

public class ExternalDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public async Task AnnotateExternalToTypeFieldCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType(o => o
                .Name("Query")
                .Field("entity")
                .Argument("id", a => a.Type<IntType>())
                .Type("User")
                .Resolve(_ => new { Id = 1 })
            )
            .AddObjectType(
                o =>
                {
                    o.Name("User")
                        .Key("id");
                    o.Field("id")
                        .Type<IntType>()
                        .Resolve(_ => 1);
                    o.Field("idCode")
                        .Type<StringType>()
                        .Resolve(_ => default!)
                        .External();
                    o.Field("address")
                        .Type("Address")
                        .Resolve(_ => default!)
                        .External();
                })
            .AddObjectType(
                o =>
                {
                    o.Name("Address");
                    o.External();
                    o.Field("street")
                        .Type<StringType>()
                        .Resolve(_ => default!);
                    o.Field("city")
                        .Type<StringType>()
                        .Resolve(_ => default!);
                })
            .BuildSchemaAsync();

        // act
        var query = schema.GetType<ObjectType>("User");
        var address = schema.GetType<ObjectType>("Address");

        // assert
        Assert.Collection(
            query.Fields["idCode"].Directives,
            item => Assert.Equal(FederationTypeNames.ExternalDirective_Name, item.Type.Name));
        Assert.Collection(
            query.Fields["address"].Directives,
            item => Assert.Equal(FederationTypeNames.ExternalDirective_Name, item.Type.Name));
        Assert.Collection(
            address.Directives,
            item => Assert.Equal(FederationTypeNames.ExternalDirective_Name, item.Type.Name));
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateExternalToTypeFieldAnnotationBased()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        // act
        var query = schema.GetType<ObjectType>("User");
        var address = schema.GetType<ObjectType>("Address");

        // assert
        Assert.Collection(
            query.Fields["idCode"].Directives,
            item => Assert.Equal(FederationTypeNames.ExternalDirective_Name, item.Type.Name));
        Assert.Collection(
            query.Fields["address"].Directives,
            item => Assert.Equal(FederationTypeNames.ExternalDirective_Name, item.Type.Name));
        Assert.Collection(
            address.Directives,
            item => Assert.Equal(FederationTypeNames.ExternalDirective_Name, item.Type.Name));
        schema.MatchSnapshot();
    }
}

public class Query
{
    public User GetEntity(int id) => default!;
}

public class User
{
    [Key]
    public int Id { get; set; }
    [External]
    public string IdCode { get; set; } = default!;
    [External]
    public Address Address { get; set; } = default!;
}

[External]
public class Address
{
    public string Street { get; } = default!;
    public string City { get; } = default!;
}
