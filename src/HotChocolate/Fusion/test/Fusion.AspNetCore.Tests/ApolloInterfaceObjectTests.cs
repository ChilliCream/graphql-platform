using HotChocolate.ApolloFederation.Types;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class ApolloInterfaceObjectTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_HideInaccessibleImplementor_When_InterfaceObjectStandsInForInterface()
    {
        // arrange
        using var catalog = CreateSourceSchema(
            "catalog",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<HiddenImplementorCatalog.QueryType>()
                .AddType<HiddenImplementorCatalog.ItemType>()
                .AddType<HiddenImplementorCatalog.VisibleType>()
                .AddType<HiddenImplementorCatalog.PrivateType>());
        using var extra = CreateSourceSchema(
            "extra",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<HiddenImplementorExtra.QueryType>()
                .AddType<HiddenImplementorExtra.ItemType>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("catalog", catalog), ("extra", extra)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);

        // act
        using var responses = await ExecuteAsync(
            gateway,
            ("interface-object",
                """
                {
                  items {
                    id
                    common
                    extra
                  }
                  alive
                }
                """),
            ("common-only",
                """
                {
                  items {
                    id
                    common
                  }
                  alive
                }
                """),
            ("opaque-entry",
                """
                {
                  extraItems {
                    id
                    extra
                  }
                  alive
                }
                """),
            ("opaque-entry-typename",
                """
                {
                  extraItems {
                    __typename
                    id
                    extra
                  }
                  alive
                }
                """));

        // assert
        responses.ToSnapshot().MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Execute_Should_HideInaccessibleImplementor_When_OnlyOtherImplementorIsInaccessible()
    {
        // arrange
        using var accounts = CreateSourceSchema(
            "accounts",
            builder => builder
                .AddApolloFederation()
                .AddQueryType(d => d.Name("Query"))
                .AddType<SingleHiddenImplementorAccounts.AccountType>()
                .AddType<SingleHiddenImplementorAccounts.PersonalType>()
                .AddType<SingleHiddenImplementorAccounts.InternalType>());
        using var ledger = CreateSourceSchema(
            "ledger",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<SingleHiddenImplementorLedger.QueryType>()
                .AddType<SingleHiddenImplementorLedger.AccountType>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("accounts", accounts), ("ledger", ledger)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);

        // act
        using var responses = await ExecuteAsync(
            gateway,
            ("typename",
                """
                {
                  rows {
                    account {
                      __typename
                      id
                      balance
                    }
                  }
                }
                """),
            ("no-typename",
                """
                {
                  rows {
                    account {
                      id
                      balance
                    }
                  }
                }
                """),
            ("fragment",
                """
                {
                  rows {
                    account {
                      id
                      ... on Personal {
                        name
                      }
                    }
                  }
                }
                """));

        // assert
        responses.ToSnapshot().MatchMarkdownSnapshot();
    }

    private static async Task<NamedResponses> ExecuteAsync(
        Gateway gateway,
        params (string Name, string Operation)[] operations)
    {
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var responses = new NamedResponses();

        foreach (var (name, operation) in operations)
        {
            using var result = await client.PostAsync(
                new OperationRequest(operation),
                new Uri("http://localhost:5000/graphql"),
                TestContext.Current.CancellationToken);
            responses.Add(name, await result.ReadAsResultAsync(TestContext.Current.CancellationToken));
        }

        return responses;
    }

    private sealed class NamedResponses : IDisposable
    {
        private readonly List<(string Name, OperationResult Response)> _responses = [];

        public void Add(string name, OperationResult response) => _responses.Add((name, response));

        public Snapshot ToSnapshot()
        {
            var snapshot = Snapshot.Create();

            foreach (var (name, response) in _responses)
            {
                snapshot.Add(response, name);
            }

            return snapshot;
        }

        public void Dispose()
        {
            foreach (var (_, response) in _responses)
            {
                response.Dispose();
            }
        }
    }

    // catalog owns the entity interface Item with the public Visible and the hidden Private.
    public static class HiddenImplementorCatalog
    {
        public interface IItem
        {
            string Id { get; }

            string Common { get; }
        }

        public sealed record Visible(string Id, string Common) : IItem;

        [Inaccessible]
        public sealed record Private(string Id, string Common) : IItem;

        private static readonly IItem[] s_items =
        [
            new Visible("v1", "catalog-v1"),
            new Private("p1", "catalog-p1"),
            new Visible("v2", "catalog-v2")
        ];

        public sealed class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name(OperationTypeNames.Query);
                descriptor.Field("items").Type<ListType<ItemType>>().Resolve(_ => s_items);
                descriptor.Field("alive").Type<StringType>().Resolve("catalog-alive");
            }
        }

        public sealed class ItemType : InterfaceType<IItem>
        {
            protected override void Configure(IInterfaceTypeDescriptor<IItem> descriptor)
            {
                descriptor.Name("Item");
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveById(default!));
                descriptor.Field(i => i.Id).Type<NonNullType<IdType>>();
                descriptor.Field(i => i.Common).Type<StringType>();
            }

            private static IItem? ResolveById(string id)
                => s_items.FirstOrDefault(i => i.Id == id);
        }

        public sealed class VisibleType : ObjectType<Visible>
        {
            protected override void Configure(IObjectTypeDescriptor<Visible> descriptor)
            {
                descriptor.Name("Visible");
                descriptor.Implements<ItemType>();
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveById(default!));
                descriptor.Field(i => i.Id).Type<NonNullType<IdType>>();
                descriptor.Field(i => i.Common).Type<StringType>();
            }

            private static Visible? ResolveById(string id)
                => s_items.OfType<Visible>().FirstOrDefault(i => i.Id == id);
        }

        public sealed class PrivateType : ObjectType<Private>
        {
            protected override void Configure(IObjectTypeDescriptor<Private> descriptor)
            {
                descriptor.Name("Private");
                descriptor.Implements<ItemType>();
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveById(default!));
                descriptor.Field(i => i.Id).Type<NonNullType<IdType>>();
                descriptor.Field(i => i.Common).Type<StringType>();
            }

            private static Private? ResolveById(string id)
                => s_items.OfType<Private>().FirstOrDefault(i => i.Id == id);
        }
    }

    // extra stands in for Item and lists the same three items.
    public static class HiddenImplementorExtra
    {
        public sealed record Item(string Id)
        {
            public string Extra => $"extra-{Id}";
        }

        public sealed class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name(OperationTypeNames.Query);
                descriptor
                    .Field("extraItems")
                    .Type<ListType<ItemType>>()
                    .Resolve(_ => new[] { "v1", "p1", "v2" }.Select(id => new Item(id)));
            }
        }

        public sealed class ItemType : ObjectType<Item>
        {
            protected override void Configure(IObjectTypeDescriptor<Item> descriptor)
            {
                descriptor.Name("Item");
                descriptor.InterfaceObject();
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveById(default!));
                descriptor.Field(i => i.Id).Type<NonNullType<IdType>>();
                descriptor.Field(i => i.Extra).Type<StringType>();
            }

            private static Item ResolveById(string id) => new(id);
        }
    }

    // accounts owns the entity interface Account with the public Personal and the hidden
    // Internal, and holds the personal accounts a1 and a2 and the internal account a3.
    public static class SingleHiddenImplementorAccounts
    {
        public interface IAccount
        {
            string Id { get; }

            string Name { get; }
        }

        public sealed record Personal(string Id, string Name) : IAccount;

        [Inaccessible]
        public sealed record Internal(string Id, string Name) : IAccount;

        private static readonly IAccount[] s_accounts =
        [
            new Personal("a1", "Ann"),
            new Personal("a2", "Abe"),
            new Internal("a3", "Ida")
        ];

        public sealed class AccountType : InterfaceType<IAccount>
        {
            protected override void Configure(IInterfaceTypeDescriptor<IAccount> descriptor)
            {
                descriptor.Name("Account");
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveById(default!));
                descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
                descriptor.Field(a => a.Name).Type<StringType>();
            }

            private static IAccount? ResolveById(string id)
                => s_accounts.FirstOrDefault(a => a.Id == id);
        }

        public sealed class PersonalType : ObjectType<Personal>
        {
            protected override void Configure(IObjectTypeDescriptor<Personal> descriptor)
            {
                descriptor.Name("Personal");
                descriptor.Implements<AccountType>();
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveById(default!));
                descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
                descriptor.Field(a => a.Name).Type<StringType>();
            }

            private static Personal? ResolveById(string id)
                => s_accounts.OfType<Personal>().FirstOrDefault(a => a.Id == id);
        }

        public sealed class InternalType : ObjectType<Internal>
        {
            protected override void Configure(IObjectTypeDescriptor<Internal> descriptor)
            {
                descriptor.Name("Internal");
                descriptor.Implements<AccountType>();
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveById(default!));
                descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
                descriptor.Field(a => a.Name).Type<StringType>();
            }

            private static Internal? ResolveById(string id)
                => s_accounts.OfType<Internal>().FirstOrDefault(a => a.Id == id);
        }
    }

    // ledger stands in for Account and lists the balances of a1, a2 and a3.
    public static class SingleHiddenImplementorLedger
    {
        public sealed record Account(string Id, int Balance);

        public sealed record Row(Account Account);

        private static readonly Account[] s_accounts =
        [
            new Account("a1", 11),
            new Account("a2", 22),
            new Account("a3", 33)
        ];

        public sealed class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name(OperationTypeNames.Query);
                descriptor
                    .Field("rows")
                    .Type<ListType<RowType>>()
                    .Resolve(_ => s_accounts.Select(a => new Row(a)));
            }
        }

        public sealed class RowType : ObjectType<Row>
        {
            protected override void Configure(IObjectTypeDescriptor<Row> descriptor)
            {
                descriptor.Name("Row");
                descriptor.Field(r => r.Account).Type<AccountType>();
            }
        }

        public sealed class AccountType : ObjectType<Account>
        {
            protected override void Configure(IObjectTypeDescriptor<Account> descriptor)
            {
                descriptor.Name("Account");
                descriptor.InterfaceObject();
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveById(default!));
                descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
                descriptor.Field(a => a.Balance).Type<IntType>();
            }

            private static Account? ResolveById(string id)
                => s_accounts.FirstOrDefault(a => a.Id == id);
        }
    }
}
