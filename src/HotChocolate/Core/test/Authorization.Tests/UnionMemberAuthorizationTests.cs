using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Authorization;

public class UnionMemberAuthorizationTests
{
    [Fact]
    public async Task Union_Should_ApplyMemberTypeAuthorization_When_DeclaredInSchemaDocument()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(
                """
                type Query {
                  search: SearchResult
                }

                type Invoice @authorize {
                  amount: Int!
                }

                type Customer {
                  name: String!
                }

                union SearchResult = Invoice | Customer
                """)
            .AddResolver("Query", "search", _ => new Invoice())
            .BindRuntimeType<Invoice>()
            .BindRuntimeType<Customer>()
            .AddAuthorizationHandler<DenyAll>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ search { ... on Invoice { amount } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "search"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "search": null
              }
            }
            """);
    }

    [Fact]
    public async Task Union_Should_ApplyMemberTypeAuthorization_When_CreatedByQueryConventions()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddQueryConventions()
            .AddAuthorizationHandler<DenyAll>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ invoice { ... on Invoice { amount } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "invoice"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Union_Should_AuthorizeMemberTypeOnce_When_MemberIsDeclaredTwice()
    {
        // arrange
        var handler = new CountingAllowAll();
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d
                .Name("Query")
                .Field("search")
                .Type<SearchResultType>()
                .Resolve(new Invoice()))
            .AddType<Customer>()
            .AddAuthorizationHandler(_ => handler)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await executor.ExecuteAsync(
            "{ search { __typename } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, handler.Calls);
    }

    public sealed class SearchResultType : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("SearchResult");
            descriptor.Type<ObjectType<Invoice>>();
            descriptor.Type(new NamedTypeNode("Invoice"));
            descriptor.Type<ObjectType<Customer>>();
        }
    }

    public sealed class Query
    {
        [Error<InvoiceNotFoundException>]
        public Invoice GetInvoice() => new();
    }

    [Authorize]
    public sealed class Invoice
    {
        public int Amount => 250;
    }

    public sealed class Customer
    {
        public string Name => "Ada";
    }

    public sealed class InvoiceNotFoundException : Exception;

    private sealed class CountingAllowAll : IAuthorizationHandler
    {
        private int _calls;

        public int Calls => _calls;

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            return new(AuthorizeResult.Allowed);
        }

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            AuthorizationContext context,
            IReadOnlyList<AuthorizeDirective> directives,
            CancellationToken cancellationToken = default)
            => new(AuthorizeResult.Allowed);
    }

    private sealed class DenyAll : IAuthorizationHandler
    {
        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive,
            CancellationToken cancellationToken = default)
            => new(AuthorizeResult.NotAllowed);

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            AuthorizationContext context,
            IReadOnlyList<AuthorizeDirective> directives,
            CancellationToken cancellationToken = default)
            => new(AuthorizeResult.NotAllowed);
    }
}
