using HotChocolate.ApolloFederation.Types;
using HotChocolate.Authorization;
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
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        var entityType = schema.Types.GetType<ObjectType>(ServiceType_Name);
        var sdlResolver = entityType.Fields[WellKnownFieldNames.Sdl].Resolver!;

        // act
        var value = await sdlResolver(CreateResolverContext(schema));

        // assert
        Utf8GraphQLParser
            .Parse((string)value!)
            .MatchInlineSnapshot(
                """
                schema
                  @link(
                    url: "https://specs.apollo.dev/federation/v2.6"
                    import: ["@key", "@tag", "FieldSet"]
                  ) {
                  query: Query
                }

                type Query {
                  _service: _Service!
                  _entities(representations: [_Any!]!): [_Entity]!
                }

                "This type provides a field named sdl: String! which exposes the SDL of the service's schema. This SDL (schema definition language) is a printed version of the service's schema including the annotations of federation directives. This SDL does not include the additions of the federation spec."
                type _Service {
                  sdl: String!
                }

                type Address @key(fields: "matchCode") {
                  matchCode: String
                }

                "Union of all types that key directive applied. This information is needed by the Apollo federation gateway."
                union _Entity = Address

                "The _Any scalar is used to pass representations of entities from external services into the root _entities field for execution. Validation of the _Any scalar is done by matching the __typename and @external fields defined in the schema."
                scalar _Any

                "Scalar representing a set of fields."
                scalar FieldSet

                "Used to indicate a combination of fields that can be used to uniquely identify and fetch an object or interface."
                directive @key(fields: FieldSet!, resolvable: Boolean = true) repeatable on
                  | OBJECT
                  | INTERFACE

                "Links definitions within the document to external schemas."
                directive @link(
                  "Gets imported specification url."
                  url: String!
                  "Gets optional list of imported element names."
                  import: [String!]
                ) repeatable on SCHEMA
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
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        var entityType = schema.Types.GetType<ObjectType>(ServiceType_Name);
        var sdlResolver = entityType.Fields[WellKnownFieldNames.Sdl].Resolver!;

        // act
        var value = await sdlResolver(CreateResolverContext(schema));

        // assert
        Utf8GraphQLParser
            .Parse((string)value!)
            .MatchInlineSnapshot(
                """
                schema
                  @link(
                    url: "https://specs.apollo.dev/federation/v2.2"
                    import: ["@key", "@tag", "FieldSet"]
                  ) {
                  query: Query
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

                type Address @key(fields: "matchCode") {
                  matchCode: String
                }

                "Union of all types that key directive applied. This information is needed by the Apollo federation gateway."
                union _Entity = Address

                "The _Any scalar is used to pass representations of entities from external services into the root _entities field for execution. Validation of the _Any scalar is done by matching the __typename and @external fields defined in the schema."
                scalar _Any

                "Scalar representing a set of fields."
                scalar FieldSet

                "Used to indicate a combination of fields that can be used to uniquely identify and fetch an object or interface."
                directive @key(fields: FieldSet!, resolvable: Boolean = true) repeatable on
                  | OBJECT
                  | INTERFACE

                "Links definitions within the document to external schemas."
                directive @link(
                  "Gets imported specification url."
                  url: String!
                  "Gets optional list of imported element names."
                  import: [String!]
                ) repeatable on SCHEMA
                """);
    }

    [Fact]
    public async Task GetSdl_Should_OmitInternalDirectives_When_FieldIsAuthorized()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddAuthorizationCore()
            .AddApolloFederation()
            .AddQueryType<AuthorizedQuery>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        var serviceType = schema.Types.GetType<ObjectType>(ServiceType_Name);
        var sdlResolver = serviceType.Fields[WellKnownFieldNames.Sdl].Resolver!;

        // act
        var value = await sdlResolver(CreateResolverContext(schema));

        // assert
        Utf8GraphQLParser
            .Parse((string)value!)
            .MatchInlineSnapshot(
                """
                schema
                  @link(
                    url: "https://specs.apollo.dev/federation/v2.6"
                    import: ["@key", "@tag", "FieldSet"]
                  ) {
                  query: AuthorizedQuery
                }

                type AuthorizedQuery {
                  address(id: Int!): Address!
                  _service: _Service!
                  _entities(representations: [_Any!]!): [_Entity]!
                }

                "This type provides a field named sdl: String! which exposes the SDL of the service's schema. This SDL (schema definition language) is a printed version of the service's schema including the annotations of federation directives. This SDL does not include the additions of the federation spec."
                type _Service {
                  sdl: String!
                }

                type Address @key(fields: "matchCode") {
                  matchCode: String
                }

                "Union of all types that key directive applied. This information is needed by the Apollo federation gateway."
                union _Entity = Address

                "Defines when a policy shall be executed."
                enum ApplyPolicy {
                  "Before the resolver was executed."
                  BEFORE_RESOLVER
                  "After the resolver was executed."
                  AFTER_RESOLVER
                  "The policy is applied in the validation step before the execution."
                  VALIDATION
                }

                "The _Any scalar is used to pass representations of entities from external services into the root _entities field for execution. Validation of the _Any scalar is done by matching the __typename and @external fields defined in the schema."
                scalar _Any

                "Scalar representing a set of fields."
                scalar FieldSet

                "Used to indicate a combination of fields that can be used to uniquely identify and fetch an object or interface."
                directive @key(fields: FieldSet!, resolvable: Boolean = true) repeatable on
                  | OBJECT
                  | INTERFACE

                "Links definitions within the document to external schemas."
                directive @link(
                  "Gets imported specification url."
                  url: String!
                  "Gets optional list of imported element names."
                  import: [String!]
                ) repeatable on SCHEMA
                """);
    }

    [Fact]
    public async Task GetSdl_Should_IncludeInternalDirectives_When_InternalDirectivesAreDisabled()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddAuthorizationCore()
            .AddApolloFederation()
            .AddQueryType<AuthorizedQuery>()
            .ModifyOptions(o => o.DisableInternalDirectives = true)
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        var serviceType = schema.Types.GetType<ObjectType>(ServiceType_Name);
        var sdlResolver = serviceType.Fields[WellKnownFieldNames.Sdl].Resolver!;

        // act
        var value = await sdlResolver(CreateResolverContext(schema));

        // assert
        Utf8GraphQLParser
            .Parse((string)value!)
            .MatchInlineSnapshot(
                """
                schema
                  @link(
                    url: "https://specs.apollo.dev/federation/v2.6"
                    import: ["@key", "@tag", "FieldSet"]
                  ) {
                  query: AuthorizedQuery
                }

                type AuthorizedQuery {
                  address(id: Int!): Address! @authorize
                  _service: _Service!
                  _entities(representations: [_Any!]!): [_Entity]!
                }

                "This type provides a field named sdl: String! which exposes the SDL of the service's schema. This SDL (schema definition language) is a printed version of the service's schema including the annotations of federation directives. This SDL does not include the additions of the federation spec."
                type _Service {
                  sdl: String!
                }

                type Address @key(fields: "matchCode") {
                  matchCode: String
                }

                "Union of all types that key directive applied. This information is needed by the Apollo federation gateway."
                union _Entity = Address

                "Defines when a policy shall be executed."
                enum ApplyPolicy {
                  "Before the resolver was executed."
                  BEFORE_RESOLVER
                  "After the resolver was executed."
                  AFTER_RESOLVER
                  "The policy is applied in the validation step before the execution."
                  VALIDATION
                }

                "The _Any scalar is used to pass representations of entities from external services into the root _entities field for execution. Validation of the _Any scalar is done by matching the __typename and @external fields defined in the schema."
                scalar _Any

                "Scalar representing a set of fields."
                scalar FieldSet

                "The authorize directive."
                directive @authorize(
                  "The name of the authorization policy that determines access to the annotated resource."
                  policy: String
                  "Roles that are allowed to access the annotated resource."
                  roles: [String!]
                  "Defines when the authorize directive shall be applied. By default the authorize directive is applied before the resolver is executed."
                  apply: ApplyPolicy! = BEFORE_RESOLVER
                ) repeatable on OBJECT | FIELD_DEFINITION

                "Used to indicate a combination of fields that can be used to uniquely identify and fetch an object or interface."
                directive @key(fields: FieldSet!, resolvable: Boolean = true) repeatable on
                  | OBJECT
                  | INTERFACE

                "Links definitions within the document to external schemas."
                directive @link(
                  "Gets imported specification url."
                  url: String!
                  "Gets optional list of imported element names."
                  import: [String!]
                ) repeatable on SCHEMA
                """);
    }

    [Fact]
    public async Task GetSdl_Should_ReturnSameSdl_When_CalledTwiceForOneSchema()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        var service = new _Service();

        // act
        var first = service.GetSdl(schema);
        var second = service.GetSdl(schema);

        // assert
        Assert.Same(first, second);
    }

    [Fact]
    public async Task Sdl_Should_ReturnError_When_SelectedTwiceInOneRequest()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              _service {
                first: sdl
                second: sdl
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The field `_Service.sdl` can only be resolved once per request.",
                  "path": [
                    "_service",
                    "second"
                  ]
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Sdl_Should_ReturnError_When_SelectedThroughTwoServiceFields()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              first: _service {
                sdl
              }
              second: _service {
                sdl
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        var error = Assert.Single(operationResult.Errors);
        Assert.Equal(
            "The field `_Service.sdl` can only be resolved once per request.",
            error.Message);
        Assert.True(operationResult.Data?.IsValueNull);
    }

    [Fact]
    public async Task Sdl_Should_ResolveInEveryRequest_When_SelectedOncePerRequest()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var first = await executor.ExecuteAsync(
            "{ _service { sdl } }",
            TestContext.Current.CancellationToken);
        var second = await executor.ExecuteAsync(
            "{ _service { sdl } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(first.ExpectOperationResult().Errors);
        Assert.Empty(second.ExpectOperationResult().Errors);
    }

    public class Query
    {
        public Address GetAddress(int id) => null!;
    }

    public class AuthorizedQuery
    {
        [Authorize]
        public Address GetAddress(int id) => null!;
    }

    public class Address
    {
        [Key] public string? MatchCode { get; set; }
    }
}
