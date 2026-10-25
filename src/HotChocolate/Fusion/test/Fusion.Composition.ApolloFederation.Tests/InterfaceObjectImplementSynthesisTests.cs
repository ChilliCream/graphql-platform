using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class InterfaceObjectImplementSynthesisTests
{
    // Apollo Federation has no @implement marker. When an implementing type redeclares a field
    // that an @interfaceObject stand-in also contributes, Apollo relies on @shareable to make the
    // field resolvable from both subgraphs. The connector bridges this to the Composite Schema
    // Spec by synthesizing @implement onto the shareable declaration, so a valid Apollo subgraph
    // composes. A non-shareable redeclaration is left untouched: the native
    // INTERFACE_OBJECT_FIELD_REQUIRES_IMPLEMENT rule rejects it, mirroring Apollo's own rejection
    // of a field resolvable from two subgraphs without @shareable.
    [Fact]
    public void Compose_Should_Succeed_When_ImplementingFieldIsShareable()
    {
        // arrange
        var log = new CompositionLog();
        var result = Compose(Accounts(isActiveShareable: true), Activity(isActiveShareable: true), log);

        // assert
        Assert.True(
            result.IsSuccess,
            result.IsSuccess ? null : result.Errors[0].Message);
    }

    [Fact]
    public void Compose_Should_Error_When_ImplementingFieldIsNotShareable()
    {
        // arrange
        var log = new CompositionLog();
        var result = Compose(Accounts(isActiveShareable: false), Activity(isActiveShareable: true), log);

        // assert
        Assert.True(result.IsFailure);
        Assert.Contains(
            log,
            e => e.Code == LogEntryCodes.InterfaceObjectFieldRequiresImplement
                && e.Message.Contains("isActive"));
    }

    private static CompositionResult<MutableSchemaDefinition> Compose(
        string accounts,
        string activity,
        CompositionLog log)
        => new SchemaComposer(
            [
                new SourceSchemaText("accounts", accounts),
                new SourceSchemaText("activity", activity)
            ],
            new SchemaComposerOptions(),
            log).Compose();

    // Subgraph that owns the Account interface and its Admin implementer. Admin redeclares the
    // isActive field the activity subgraph contributes through @interfaceObject.
    private static string Accounts(bool isActiveShareable)
        => $$"""
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@shareable"]) {
              query: Query
            }

            type Query {
              accounts: [Account!]!
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            interface Account @key(fields: "id") {
              id: ID!
            }

            type Admin implements Account @key(fields: "id") {
              id: ID!
              isActive: Boolean!{{(isActiveShareable ? " @shareable" : "")}}
            }

            type _Service { sdl: String! }

            union _Entity = Admin

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @shareable on OBJECT | FIELD_DEFINITION
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

    // Stand-in subgraph that contributes isActive to the Account interface via @interfaceObject.
    private static string Activity(bool isActiveShareable)
        => $$"""
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@interfaceObject", "@shareable"]) {
              query: Query
            }

            type Query {
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }

            type Account @key(fields: "id") @interfaceObject {
              id: ID!
              isActive: Boolean!{{(isActiveShareable ? " @shareable" : "")}}
            }

            type _Service { sdl: String! }

            union _Entity = Account

            scalar FieldSet
            scalar _Any

            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @interfaceObject on OBJECT
            directive @shareable on OBJECT | FIELD_DEFINITION
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;
}
