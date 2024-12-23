using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class SchemaTests
{
    [Fact]
    public async Task Allow_CacheControl_On_FieldDefinition()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer(disableDefaultSecurity: true)
                .AddTypeExtension(typeof(Query))
                .ConfigureSchema(
                    b => b.TryAddRootType(
                        () => new ObjectType(
                            d => d.Name(OperationTypeNames.Query)),
                        Language.OperationType.Query)
                        .ModifyOptions(o => o.RemoveUnusedTypeSystemDirectives = false))
                .AddCacheControl()
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query
            }

            type Book {
              title: String! @cacheControl(maxAge: 5000)
              description: String!
            }

            type Query {
              book: Book! @cacheControl(maxAge: 0)
            }

            "The scope of a cache hint."
            enum CacheControlScope {
              "The value to cache is not tied to a single user."
              PUBLIC
              "The value to cache is specific to a single user."
              PRIVATE
            }

            "The `@cacheControl` directive may be provided for individual fields or entire object, interface or union types to provide caching hints to the executor."
            directive @cacheControl("The maximum amount of time this field's cached value is valid, in seconds." maxAge: Int "The maximum amount of time this field's cached value is valid in shared caches like CDNs, in seconds." sharedMaxAge: Int "If `true`, the field inherits the `maxAge` of its parent field." inheritMaxAge: Boolean "If `PRIVATE`, the field's value is specific to a single user. The default value is `PUBLIC`, which means the field's value is not tied to a single user." scope: CacheControlScope "The Vary HTTP response header describes the parts of the request message aside from the method and URL that influenced the content of the response it occurs in. Most often, this is used to create a cache key when content negotiation is in use." vary: [String]) on OBJECT | FIELD_DEFINITION | INTERFACE | UNION

            """
            The `@oneOf` directive is used within the type system definition language
             to indicate:

             - an Input Object is a Oneof Input Object, or
             - an Object Type's Field is a Oneof Field.
            """
            directive @oneOf on INPUT_OBJECT

            """
            The @tag directive is used to apply arbitrary string
            metadata to a schema location. Custom tooling can use
            this metadata during any step of the schema delivery flow,
            including composition, static analysis, and documentation.

            interface Book {
              id: ID! @tag(name: "your-value")
              title: String!
              author: String!
            }
            """
            directive @tag("The name of the tag." name: String!) repeatable on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
            """");
    }

    [QueryType]
    public static class Query
    {
        public static Book GetBook()
            => new Book("C# in depth.", "abc");
}

    public record Book(
        [property: CacheControl(5000)] string Title,
        string Description);
}
