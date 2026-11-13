using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class ContextTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_ResolveAmountWithCurrencyOfUser_When_SingleUserReadsContext()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<Billing.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              me {
                transactions {
                  amount
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Execute_Should_ResolveAmountWithCurrencyOfOwnUser_When_ListOfUsersHasDifferentCurrencies()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<Billing.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              users {
                transactions {
                  amount
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Execute_Should_ResolveAmountWithContextField_When_UserCurrencyComesFromSecondSourceSchema()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<Billing.Query>()
                .AddObjectType<Billing.User>(d => d.Ignore(u => u.UserCurrency)));

        var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<UserCurrencies.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              users {
                transactions {
                  amount
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Execute_Should_ResolveTotal_When_RequirementMixesContextValueAndEntityValue()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<Billing.Query>()
                .AddTypeExtension<Billing.TransactionTotalExtension>());

        var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<TaxRates.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              users {
                transactions {
                  total
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Execute_Should_ResolveAmountsAtBothDepths_When_TransactionsAreReachedAtDifferentDepths()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<Billing.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              users {
                transactions {
                  amount
                }
                account {
                  transactions {
                    amount
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Execute_Should_ResolveAmountsWithCurrencyOfOwnUser_When_TwoTargetsShareOneLookup()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<Billing.Query>());

        var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<PendingTransactions.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              users {
                transactions {
                  amount
                }
                pending {
                  amount
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    private static class Billing
    {
        private static readonly User[] s_users =
        [
            new User(1, new Currency("USD"), [1, 2], [5]),
            new User(2, new Currency("EUR"), [3, 4], [6])
        ];

        public class Query
        {
            public User? GetMe() => s_users[0];

            public User[] GetUsers() => s_users;

            [Lookup]
            [Internal]
            public Transaction? GetTransactionById([ID] int id) => new Transaction(id);
        }

        [EntityKey("id")]
        [Context("userContext")]
        public record User(
            [property: ID, Shareable] int Id,
            Currency UserCurrency,
            [property: GraphQLIgnore] int[] TransactionIds,
            [property: GraphQLIgnore] int[] AccountTransactionIds)
        {
            public Transaction[] GetTransactions()
                => TransactionIds.Select(id => new Transaction(id)).ToArray();

            public Account GetAccount()
                => new Account(AccountTransactionIds);
        }

        public record Account([property: GraphQLIgnore] int[] TransactionIds)
        {
            public Transaction[] GetTransactions()
                => TransactionIds.Select(id => new Transaction(id)).ToArray();
        }

        public record Transaction([property: ID, Shareable] int Id)
        {
            public int GetAmount([Require("$userContext.userCurrency.isoCode")] string currency)
                => Id * 100 * Rate(currency);

            public static int Rate(string currency)
                => currency switch
                {
                    "USD" => 1,
                    "EUR" => 2,
                    _ => throw new ArgumentException($"Unknown currency {currency}.")
                };
        }

        [ExtendObjectType<Transaction>]
        public class TransactionTotalExtension
        {
            public int GetTotal(
                [Parent] Transaction transaction,
                [Require("{ currency: $userContext.userCurrency.isoCode, taxRate: taxRate }")]
                PricingInput pricing)
                => transaction.Id * 100 * Transaction.Rate(pricing.Currency) + pricing.TaxRate;
        }

        public record Currency(string IsoCode);

        public class PricingInput
        {
            public required string Currency { get; set; }

            public required int TaxRate { get; set; }
        }
    }

    private static class UserCurrencies
    {
        private static readonly Dictionary<int, Currency> s_currencies = new()
        {
            { 1, new Currency("USD") },
            { 2, new Currency("EUR") }
        };

        public class Query
        {
            [Lookup]
            [Internal]
            public User? GetUserById([ID] int id) => new User(id, s_currencies[id]);
        }

        public record User([property: ID, Shareable] int Id, Currency UserCurrency);

        public record Currency(string IsoCode);
    }

    private static class PendingTransactions
    {
        private static readonly Dictionary<int, int[]> s_pendingIds = new()
        {
            { 1, [7] },
            { 2, [8] }
        };

        public class Query
        {
            [Lookup]
            [Internal]
            public User? GetUserById([ID] int id) => new User(id, s_pendingIds[id]);
        }

        [EntityKey("id")]
        public record User(
            [property: ID, Shareable] int Id,
            [property: GraphQLIgnore] int[] PendingIds)
        {
            public Transaction[] GetPending()
                => PendingIds.Select(id => new Transaction(id)).ToArray();
        }

        [EntityKey("id")]
        public record Transaction([property: ID, Shareable] int Id);
    }

    private static class TaxRates
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Transaction? GetTransactionById([ID] int id) => new Transaction(id);
        }

        public record Transaction([property: ID, Shareable] int Id)
        {
            public int TaxRate => Id;
        }
    }
}
