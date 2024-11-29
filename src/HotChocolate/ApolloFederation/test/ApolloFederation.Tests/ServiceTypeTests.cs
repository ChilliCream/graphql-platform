using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.TestHelper;

namespace HotChocolate.ApolloFederation;

public class ServiceTypeTests
{
    [Fact]
    public async Task TestServiceTypeEmptyQueryTypePureCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType()
            .AddType<Address>()
            .BuildSchemaAsync();

        var entityType = schema.GetType<ObjectType>(ServiceType_Name);
        var sdlResolver = entityType.Fields[WellKnownFieldNames.Sdl].Resolver!;

        // act
        var value = await sdlResolver(CreateResolverContext(schema));

        // assert
        Utf8GraphQLParser
            .Parse((string)value!)
            .MatchInlineSnapshot(
                """
                schema @link(url: "https:\/\/specs.apollo.dev\/federation\/v2.6", import: [ "@key", "@tag", "FieldSet" ]) {
                  query: Query
                }

                type Address @key(fields: "matchCode") {
                  matchCode: String
                }

                type Query {
                  _service: _Service!
                  _entities(representations: [_Any!]!): [_Entity]!
                }

                "This type provides a field named sdl: String! which exposes the SDL of the service's schema. This SDL (schema definition language) is a printed version of the service's schema including the annotations of federation directives. This SDL does not include the additions of the federation spec."
                type _Service {
                  sdl: String!
                }

                "Union of all types that key directive applied. This information is needed by the Apollo federation gateway."
                union _Entity = Address

                "Used to indicate a combination of fields that can be used to uniquely identify and fetch an object or interface."
                directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE

                "Links definitions within the document to external schemas."
                directive @link("Gets imported specification url." url: String! "Gets optional list of imported element names." import: [String!]) repeatable on SCHEMA

                "Scalar representing a set of fields."
                scalar FieldSet

                "The _Any scalar is used to pass representations of entities from external services into the root _entities field for execution. Validation of the _Any scalar is done by matching the __typename and @external fields defined in the schema."
                scalar _Any
                """);
    }

    [Fact]
    public async Task TestServiceTypeTypePureCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(FederationVersion.Federation22)
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        var entityType = schema.GetType<ObjectType>(ServiceType_Name);
        var sdlResolver = entityType.Fields[WellKnownFieldNames.Sdl].Resolver!;

        // act
        var value = await sdlResolver(CreateResolverContext(schema));

        // assert
        Utf8GraphQLParser
            .Parse((string)value!)
            .MatchInlineSnapshot(
                """
                schema @link(url: "https:\/\/specs.apollo.dev\/federation\/v2.2", import: [ "@key", "@tag", "FieldSet" ]) {
                  query: Query
                }

                type Address @key(fields: "matchCode") {
                  matchCode: String
                }

                type Query {
                  address(id: Int!): Address!
                  _service: _Service!
                  _entities(representations: [_Any!]!): [_Entity]!
                }

                "This type provides a field named sdl: String! which exposes the SDL of the service's schema. This SDL (schema definition language) is a printed version of the service's schema including the annotations of federation directives. This SDL does not include the additions of the federation spec."
                type _Service {
                  sdl: String!
                }

                "Union of all types that key directive applied. This information is needed by the Apollo federation gateway."
                union _Entity = Address

                "Used to indicate a combination of fields that can be used to uniquely identify and fetch an object or interface."
                directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE

                "Links definitions within the document to external schemas."
                directive @link("Gets imported specification url." url: String! "Gets optional list of imported element names." import: [String!]) repeatable on SCHEMA

                "Scalar representing a set of fields."
                scalar FieldSet

                "The _Any scalar is used to pass representations of entities from external services into the root _entities field for execution. Validation of the _Any scalar is done by matching the __typename and @external fields defined in the schema."
                scalar _Any
                """);
    }

    public class Query
    {
        public Address GetAddress(int id) => default!;
    }

    public class Address
    {
        [Key] public string? MatchCode { get; set; }
    }
}
