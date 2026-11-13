using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion;

public sealed class ApolloContextTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_ResolveAmountOfOwnUser_When_UsersHaveDifferentCurrencies()
    {
        // arrange
        using var gateway = await CreateBillingGatewayAsync();
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              users {
                id
                transactions {
                  id
                  amountInUserCurrency
                }
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Execute_Should_SendOneEntitiesRequest_When_UsersShareTheirCurrency()
    {
        // arrange
        using var gateway = await CreateBillingGatewayAsync();
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              dollarUsers {
                transactions {
                  id
                  amountInUserCurrency
                }
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Execute_Should_ResolveAmountAndTotalOfOwnUser_When_TwoEntityTypesReadTheContext()
    {
        // arrange
        using var gateway = await CreateBillingGatewayAsync();
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              users {
                transactions {
                  id
                  amountInUserCurrency
                }
                invoices {
                  id
                  totalInUserCurrency
                }
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Execute_Should_DeliverAmountOfOwnUser_When_TheContextRootIsInsideTheDeferredFragment()
    {
        // arrange
        using var gateway = await CreateBillingGatewayAsync();
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              ... @defer {
                users {
                  transactions {
                    id
                    amountInUserCurrency
                  }
                }
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Execute_Should_DeliverAmountOfOwnUser_When_TheContextRootIsTheAnchorOfTheDeferredFragment()
    {
        // arrange
        using var gateway = await CreateBillingGatewayAsync();
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              users {
                ... @defer {
                  transactions {
                    id
                    amountInUserCurrency
                  }
                }
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Execute_Should_SendOneRequestPerCurrency_When_ALookupBatchReadsTheContext()
    {
        // arrange
        using var gateway = await CreateAmountsGatewayAsync();
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              users {
                transactions {
                  amountInUserCurrency
                }
                pending {
                  amountInUserCurrency
                }
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Execute_Should_ResolveAmountWithUnaliasedCurrency_When_ClientSelectsContextFieldWithArguments()
    {
        // arrange
        using var gateway = await CreateAmountsGatewayAsync();
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              users {
                userCurrency(region: "EU") {
                  isoCode
                }
                transactions {
                  amountInUserCurrency
                }
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    private async Task<Gateway> CreateAmountsGatewayAsync()
    {
        var users = CreateSourceSchema(
            "users",
            builder => builder
                .AddApolloFederation()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("users")
                        .Type<NonNullType<ListType<NonNullType<Amounts.UsersUserType>>>>()
                        .Resolve(_ => Amounts.Users);
                })
                .AddType<Amounts.UsersUserType>()
                .AddType<Amounts.UsersTransactionType>());

        var pending = CreateSourceSchema(
            "pending",
            builder => builder
                .AddApolloFederation()
                .AddQueryType(d => d.Name("Query"))
                .AddType<Amounts.PendingUserType>()
                .AddType<Amounts.PendingTransactionType>());

        var amounts = CreateSourceSchema(
            "amounts",
            builder => builder
                .AddApolloFederation()
                .AddQueryType(d => d.Name("Query"))
                .AddType<Amounts.AmountsUserType>()
                .AddType<Amounts.AmountsTransactionType>()
                .AddDirectiveType(Billing.CreateContextDirective())
                .AddDirectiveType(Billing.CreateFromContextDirective()));

        return await CreateCompositeSchemaAsync(
            [("users", users), ("pending", pending), ("amounts", amounts)],
            enableGlobalObjectIdentification: false);
    }

    private async Task<Gateway> CreateBillingGatewayAsync()
    {
        var billing = CreateSourceSchema(
            "billing",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<Billing.Query>(d => d.Name("Query"))
                .AddType<Billing.User>()
                .AddType<Billing.Transaction>()
                .AddType<Billing.Invoice>()
                .AddTypeExtension<Billing.UserContext>()
                .AddTypeExtension<Billing.TransactionContext>()
                .AddTypeExtension<Billing.InvoiceContext>()
                .AddDirectiveType(Billing.CreateContextDirective())
                .AddDirectiveType(Billing.CreateFromContextDirective()));

        return await CreateCompositeSchemaAsync(
            [("billing", billing)],
            enableGlobalObjectIdentification: false);
    }

    private static class Amounts
    {
        public static readonly User[] Users =
        [
            new("1", "USD", ["1", "2"]),
            new("2", "EUR", ["3", "4"]),
            new("3", "USD", ["5"])
        ];

        public sealed record User(string Id, string IsoCode, string[] TransactionIds);

        [Shareable]
        public sealed record Currency(string IsoCode);

        public sealed record Transaction(string Id);

        public sealed class UsersUserType : ObjectType<User>
        {
            protected override void Configure(IObjectTypeDescriptor<User> descriptor)
            {
                descriptor.Name("User");
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveUser(default!));
                descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
                descriptor.Field("userCurrency")
                    .Argument("region", a => a.Type<StringType>())
                    .Type<NonNullType<ObjectType<Currency>>>()
                    .Resolve(ctx => ctx.ArgumentValue<string?>("region") is null
                        ? new Currency(ctx.Parent<User>().IsoCode)
                        : new Currency("EUR"));
                descriptor.Field("transactions")
                    .Type<NonNullType<ListType<NonNullType<UsersTransactionType>>>>()
                    .Resolve(ctx => ctx.Parent<User>().TransactionIds.Select(id => new Transaction(id)));
                descriptor.Ignore(u => u.IsoCode);
                descriptor.Ignore(u => u.TransactionIds);
            }
        }

        public sealed class UsersTransactionType : ObjectType<Transaction>
        {
            protected override void Configure(IObjectTypeDescriptor<Transaction> descriptor)
            {
                descriptor.Name("Transaction");
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveTransaction(default!));
                descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
            }
        }

        public sealed class PendingUserType : ObjectType<User>
        {
            protected override void Configure(IObjectTypeDescriptor<User> descriptor)
            {
                descriptor.Name("User");
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveUser(default!));
                descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
                descriptor.Field("pending")
                    .Type<NonNullType<ListType<NonNullType<PendingTransactionType>>>>()
                    .Resolve(ctx => new[] { new Transaction($"10{ctx.Parent<User>().Id}") });
                descriptor.Ignore(u => u.IsoCode);
                descriptor.Ignore(u => u.TransactionIds);
            }
        }

        public sealed class PendingTransactionType : ObjectType<Transaction>
        {
            protected override void Configure(IObjectTypeDescriptor<Transaction> descriptor)
            {
                descriptor.Name("Transaction");
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveTransaction(default!));
                descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
            }
        }

        public sealed class AmountsUserType : ObjectType<User>
        {
            protected override void Configure(IObjectTypeDescriptor<User> descriptor)
            {
                descriptor.Name("User");
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveUser(default!));
                descriptor.Directive("context", new ArgumentNode("name", "userContext"));
                descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
                descriptor.Field("userCurrency")
                    .Argument("region", a => a.Type<StringType>())
                    .Type<NonNullType<ObjectType<Currency>>>()
                    .External()
                    .Resolve(ctx => new Currency(ctx.Parent<User>().IsoCode));
                descriptor.Ignore(u => u.IsoCode);
                descriptor.Ignore(u => u.TransactionIds);
            }
        }

        public sealed class AmountsTransactionType : ObjectType<Transaction>
        {
            protected override void Configure(IObjectTypeDescriptor<Transaction> descriptor)
            {
                descriptor.Name("Transaction");
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveTransaction(default!));
                descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
                descriptor.Field("amountInUserCurrency")
                    .Argument("currencyCode", a => a
                        .Type<StringType>()
                        .Directive(
                            "fromContext",
                            new ArgumentNode("field", "$userContext { userCurrency { isoCode } }")))
                    .Type<NonNullType<FloatType>>()
                    .Resolve(ctx => int.Parse(ctx.Parent<Transaction>().Id)
                        * 100
                        * Billing.Transaction.Rate(ctx.ArgumentValue<string?>("currencyCode")));
            }
        }

        private static User ResolveUser(string id) => Users.First(u => u.Id == id);

        private static Transaction ResolveTransaction(string id) => new(id);
    }

    private static class Billing
    {
        private static readonly User[] s_users =
        [
            new User { Id = "1", UserCurrency = new Currency("USD"), TransactionIds = ["1", "2"] },
            new User { Id = "2", UserCurrency = new Currency("EUR"), TransactionIds = ["3", "4"] },
            new User { Id = "3", UserCurrency = new Currency("USD"), TransactionIds = ["5"] }
        ];

        public sealed class Query
        {
            public IEnumerable<User> GetUsers() => s_users;

            public IEnumerable<User> GetDollarUsers() => s_users.Where(u => u.UserCurrency.IsoCode == "USD");
        }

        [GraphQLName("User")]
        [Key("id")]
        public sealed class User
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public required Currency UserCurrency { get; init; }

            [GraphQLIgnore]
            public required string[] TransactionIds { get; init; }

            public IEnumerable<Transaction> GetTransactions()
                => TransactionIds.Select(id => new Transaction { Id = id });

            public IEnumerable<Invoice> GetInvoices()
                => TransactionIds.Select(id => new Invoice { Id = id });

            [ReferenceResolver]
            public static User? ResolveReference(string id) => s_users.FirstOrDefault(u => u.Id == id);
        }

        public sealed record Currency(string IsoCode);

        [GraphQLName("Transaction")]
        [Key("id")]
        public sealed class Transaction
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public double GetAmountInUserCurrency(string? currencyCode)
                => int.Parse(Id) * 100 * Rate(currencyCode);

            [ReferenceResolver]
            public static Transaction ResolveReference(string id) => new() { Id = id };

            public static int Rate(string? currencyCode)
                => currencyCode switch
                {
                    "USD" => 1,
                    "EUR" => 2,
                    _ => throw new GraphQLException($"Unknown currency {currencyCode}.")
                };
        }

        [GraphQLName("Invoice")]
        [Key("id")]
        public sealed class Invoice
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public double GetTotalInUserCurrency(string? currencyCode)
                => int.Parse(Id) * 1000 * Transaction.Rate(currencyCode);

            [ReferenceResolver]
            public static Invoice ResolveReference(string id) => new() { Id = id };
        }

        public sealed class UserContext : ObjectTypeExtension<User>
        {
            protected override void Configure(IObjectTypeDescriptor<User> descriptor)
                => descriptor.Directive("context", new ArgumentNode("name", "userContext"));
        }

        public sealed class TransactionContext : ObjectTypeExtension<Transaction>
        {
            protected override void Configure(IObjectTypeDescriptor<Transaction> descriptor)
                => descriptor
                    .Field(t => t.GetAmountInUserCurrency(default))
                    .Argument("currencyCode", a => a.Directive(
                        "fromContext",
                        new ArgumentNode("field", "$userContext { userCurrency { isoCode } }")));
        }

        public sealed class InvoiceContext : ObjectTypeExtension<Invoice>
        {
            protected override void Configure(IObjectTypeDescriptor<Invoice> descriptor)
                => descriptor
                    .Field(t => t.GetTotalInUserCurrency(default))
                    .Argument("currencyCode", a => a.Directive(
                        "fromContext",
                        new ArgumentNode("field", "$userContext { userCurrency { isoCode } }")));
        }

        public static DirectiveType CreateContextDirective()
            => new(descriptor =>
            {
                descriptor
                    .Name("context")
                    .Location(
                        DirectiveLocation.Object
                        | DirectiveLocation.Interface
                        | DirectiveLocation.Union);
                descriptor.Argument("name").Type<NonNullType<StringType>>();
            });

        public static DirectiveType CreateFromContextDirective()
            => new(descriptor =>
            {
                descriptor.Name("fromContext").Location(DirectiveLocation.ArgumentDefinition);
                descriptor.Argument("field").Type<NonNullType<StringType>>();
            });
    }
}
