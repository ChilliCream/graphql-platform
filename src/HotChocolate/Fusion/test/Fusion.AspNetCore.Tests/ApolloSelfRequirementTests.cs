using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class ApolloSelfRequirementTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_PassExternalKeyToRequiringSubgraph_When_RequiresSelectsExternalKeyField()
    {
        // arrange
        using var requiring = CreateSourceSchema(
            "requiring",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ExternalKeyRequiring.Query>(d => d.Name("Query"))
                .AddType<ExternalKeyRequiring.Account>());
        using var other = CreateSourceSchema(
            "other",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ExternalKeyOther.Query>(d => d.Name("Query")));

        using var gateway = await CreateCompositeSchemaAsync(
            [("requiring", requiring), ("other", other)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                  accounts {
                    id
                    displayName
                  }
                }
                """),
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "accounts": [
                  {
                    "id": "a1",
                    "displayName": "Account a1"
                  },
                  {
                    "id": "a2",
                    "displayName": "Account a2"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_FetchRequiredParentFromOtherSubgraph_When_RequiresSelectsExternalParentField()
    {
        // arrange
        using var a = CreateSourceSchema(
            "a",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ExternalParentA.Query>(d => d.Name("Query"))
                .AddType<ExternalParentA.Customer>());
        using var b = CreateSourceSchema(
            "b",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ExternalParentB.Query>(d => d.Name("Query"))
                .AddType<ExternalParentB.Order>()
                .AddType<ExternalParentB.Customer>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", a), ("b", b)],
            enableGlobalObjectIdentification: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              orders {
                id
                price
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
    public async Task Execute_Should_ChainRequirements_When_CircularRequirementReadsParentFromThirdSubgraph()
    {
        // arrange
        using var a = CreateSourceSchema(
            "a",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<CircularA.Query>(d => d.Name("Query"))
                .AddType<CircularA.Author>());
        using var b = CreateSourceSchema(
            "b",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<CircularB.Query>(d => d.Name("Query"))
                .AddType<CircularB.Post>());
        using var c = CreateSourceSchema(
            "c",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<CircularC.Query>(d => d.Name("Query"))
                .AddType<CircularC.Post>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", a), ("b", b), ("c", c)],
            enableGlobalObjectIdentification: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              feed {
                id
                byNovice
                byExpert
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
    public async Task Execute_Should_FetchRequiredFieldWithArgumentFromOtherSubgraph_When_RequiringSubgraphDeclaresItExternal()
    {
        // arrange
        using var c = CreateSourceSchema(
            "c",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ArgumentC.Query>(d => d.Name("Query"))
                .AddType<ArgumentC.PostType>()
                .AddType<ArgumentC.CommentType>());
        using var d = CreateSourceSchema(
            "d",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ArgumentD.Query>(d => d.Name("Query"))
                .AddType<ArgumentD.PostType>()
                .AddType<ArgumentD.CommentType>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("c", c), ("d", d)],
            enableGlobalObjectIdentification: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              feed {
                id
                author {
                  id
                  name
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

    // requiring computes displayName from the account id it receives as an external key.
    public static class ExternalKeyRequiring
    {
        public sealed class Query;

        [Key("id")]
        public sealed class Account
        {
            [External]
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [Requires("id")]
            public string GetDisplayName() => $"Account {Id}";

            [ReferenceResolver]
            public static Account ResolveReference(string id) => new() { Id = id };
        }
    }

    // other lists the accounts a1 and a2.
    public static class ExternalKeyOther
    {
        public sealed class Query
        {
            public IEnumerable<Account> GetAccounts() => [new Account { Id = "a1" }, new Account { Id = "a2" }];
        }

        [Key("id")]
        public sealed class Account
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [ReferenceResolver]
            public static Account ResolveReference(string id) => new() { Id = id };
        }
    }

    // a lists the orders o1 and o2 and computes price as 100 minus the discount of the customer,
    // which it only receives through the representation.
    public static class ExternalParentA
    {
        public sealed class Query
        {
            public IEnumerable<Order> GetOrders() => [new Order { Id = "o1" }, new Order { Id = "o2" }];
        }

        [Key("id")]
        public sealed class Order
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [External]
            public Customer? Customer { get; set; }

            [Requires("customer { discount }")]
            public int? GetPrice()
                => Customer?.Discount is { } discount
                    ? 100 - discount
                    : throw new GraphQLException($"order {Id} reached price without a discount");

            [ReferenceResolver]
            public static Order ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        public sealed class Customer
        {
            [GraphQLType<NonNullType<IdType>>]
            public string? Id { get; set; }

            [External]
            public int? Discount { get; set; }

            [ReferenceResolver]
            public static Customer ResolveReference(string id) => new() { Id = id };
        }
    }

    // b holds the customer of each order and the discount of each customer.
    public static class ExternalParentB
    {
        public sealed class Query;

        [Key("id")]
        public sealed class Order
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public Customer? GetCustomer() => Customer.ResolveReference(Id == "o1" ? "c1" : "c2");

            [ReferenceResolver]
            public static Order ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        public sealed class Customer
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public int? Discount { get; init; }

            [ReferenceResolver]
            public static Customer ResolveReference(string id)
                => new() { Id = id, Discount = id == "c1" ? 10 : 25 };
        }
    }

    // a lists the posts p1 and p2, owns the authors and computes byExpert from byNovice.
    public static class CircularA
    {
        public sealed class Query
        {
            public IEnumerable<Post> GetFeed() => [new Post { Id = "p1" }, new Post { Id = "p2" }];
        }

        [Key("id")]
        public sealed class Post
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [External]
            [GraphQLType<NonNullType<BooleanType>>]
            public bool? ByNovice { get; set; }

            [Requires("byNovice")]
            public bool GetByExpert()
                => ByNovice is { } byNovice
                    ? !byNovice
                    : throw new GraphQLException($"post {Id} reached byExpert without byNovice");

            [ReferenceResolver]
            public static Post ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        public sealed class Author
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public required string Name { get; init; }

            public int YearsOfExperience { get; init; }

            [ReferenceResolver]
            public static Author ResolveReference(string id)
                => id == "u1"
                    ? new Author { Id = id, Name = "Uma", YearsOfExperience = 4 }
                    : new Author { Id = id, Name = "Ugo", YearsOfExperience = 21 };
        }
    }

    // b holds the author of each post.
    public static class CircularB
    {
        public sealed class Query;

        [Key("id")]
        public sealed class Post
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public Author GetAuthor() => new() { Id = Id == "p1" ? "u1" : "u2" };

            [ReferenceResolver]
            public static Post ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        public sealed class Author
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [ReferenceResolver]
            public static Author ResolveReference(string id) => new() { Id = id };
        }
    }

    // c computes byNovice from the experience of the post's author, which it only receives
    // through the representation.
    public static class CircularC
    {
        public sealed class Query;

        [Key("id")]
        public sealed class Post
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [External]
            [GraphQLNonNullType]
            public Author? Author { get; set; }

            [Requires("author { yearsOfExperience }")]
            public bool GetByNovice()
                => Author?.YearsOfExperience is { } yearsOfExperience
                    ? yearsOfExperience < 10
                    : throw new GraphQLException($"post {Id} reached byNovice without an author");

            [ReferenceResolver]
            public static Post ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        public sealed class Author
        {
            [GraphQLType<NonNullType<IdType>>]
            public string? Id { get; set; }

            [External]
            [GraphQLType<NonNullType<IntType>>]
            public int? YearsOfExperience { get; set; }

            [ReferenceResolver]
            public static Author ResolveReference(string id) => new() { Id = id };
        }
    }

    // c lists the posts p1 and p2 with their comments and the author id of each comment.
    public static class ArgumentC
    {
        public sealed record Post(string Id);

        public sealed record Comment(string Id, string AuthorId, string Body);

        private static readonly Dictionary<string, Comment[]> s_comments = new()
        {
            ["p1"] =
            [
                new Comment("k1", "u1", "first"),
                new Comment("k2", "u2", "second"),
                new Comment("k3", "u3", "third"),
                new Comment("k4", "u4", "fourth")
            ],
            ["p2"] =
            [
                new Comment("k5", "u2", "fifth"),
                new Comment("k6", "u3", "sixth"),
                new Comment("k7", "u1", "seventh")
            ]
        };

        public sealed class Query
        {
            [GraphQLType<ListType<PostType>>]
            public IEnumerable<Post> GetFeed() => [new Post("p1"), new Post("p2")];
        }

        public sealed class PostType : ObjectType<Post>
        {
            protected override void Configure(IObjectTypeDescriptor<Post> descriptor)
            {
                descriptor.Name("Post");
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveById(default!));
                descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
                descriptor
                    .Field("comments")
                    .Argument("limit", a => a.Type<NonNullType<IntType>>())
                    .Type<ListType<CommentType>>()
                    .Resolve(ctx => s_comments[ctx.Parent<Post>().Id].Take(ctx.ArgumentValue<int>("limit")));
            }

            private static Post ResolveById(string id) => new(id);
        }

        public sealed class CommentType : ObjectType<Comment>
        {
            protected override void Configure(IObjectTypeDescriptor<Comment> descriptor)
            {
                descriptor.Name("Comment");
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveById(default!));
                descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
                descriptor.Field(c => c.AuthorId).Type<IdType>();
                descriptor.Field(c => c.Body).Type<StringType>();
            }

            private static Comment? ResolveById(string id)
                => s_comments.Values.SelectMany(c => c).FirstOrDefault(c => c.Id == id);
        }
    }

    // d resolves the author of a post from the author id of its third comment, which it only
    // receives through the representation.
    public static class ArgumentD
    {
        public sealed class Post
        {
            public required string Id { get; init; }

            public IReadOnlyList<Comment>? Comments { get; init; }
        }

        public sealed class Comment
        {
            public string? Id { get; set; }

            public string? AuthorId { get; set; }
        }

        public sealed record Author(string Id, string Name);

        public sealed class Query;

        public sealed class PostType : ObjectType<Post>
        {
            protected override void Configure(IObjectTypeDescriptor<Post> descriptor)
            {
                descriptor.Name("Post");
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveById(default!, default));
                descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
                descriptor
                    .Field(p => p.Comments)
                    .External()
                    .Argument("limit", a => a.Type<NonNullType<IntType>>())
                    .Type<ListType<CommentType>>();
                descriptor
                    .Field("author")
                    .Type<ObjectType<Author>>()
                    .Requires("comments(limit: 3) { authorId }")
                    .Resolve(ctx =>
                    {
                        var post = ctx.Parent<Post>();

                        if (post.Comments is not { Count: 3 } comments)
                        {
                            throw new GraphQLException($"post {post.Id} reached author without three comments");
                        }

                        var authorId = comments[2].AuthorId!;
                        return new Author(authorId, $"Author {authorId}");
                    });
            }

            private static Post ResolveById(string id, [Map("comments")] IReadOnlyList<Comment>? comments)
                => new() { Id = id, Comments = comments };
        }

        public sealed class CommentType : ObjectType<Comment>
        {
            protected override void Configure(IObjectTypeDescriptor<Comment> descriptor)
            {
                descriptor.Name("Comment");
                descriptor.Key("id").ResolveReferenceWith(_ => ResolveById(default!));
                descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
                descriptor.Field(c => c.AuthorId).External().Type<IdType>();
            }

            private static Comment ResolveById(string id) => new() { Id = id };
        }
    }
}
