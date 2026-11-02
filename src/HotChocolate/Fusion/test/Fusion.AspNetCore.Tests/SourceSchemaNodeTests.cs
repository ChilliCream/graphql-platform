using System.Reflection;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaNodeTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_ResolveEachNodeTypeThroughItsKeyOwner_When_KeyIsExternalInOtherNodeLookupSchema()
    {
        // arrange
        // each schema answers the id of the type it declares @external with "never"
        using var a = CreateSourceSchema(
            "a",
            builder => builder
                .ModifyOptions(o => o.InferKeysFromLookups = false)
                .AddQueryType<NodeLookupA.Query>(d => d.Name("Query"))
                .AddType<NodeLookupA.Account>()
                .AddType<NodeLookupA.Chat>());
        using var b = CreateSourceSchema(
            "b",
            builder => builder
                .ModifyOptions(o => o.InferKeysFromLookups = false)
                .AddQueryType<NodeLookupB.Query>(d => d.Name("Query"))
                .AddType<NodeLookupB.Account>()
                .AddType<NodeLookupB.Chat>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", a), ("b", b)],
            includeOperationPlan: false,
            configureComposerOptions: o => o.Merger.NodeResolution = NodeResolution.SourceSchema);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        (string Name, string Operation)[] operations =
        [
            ("00-node-id",
                """
                query NodeId {
                  node(id: "a1") {
                    id
                  }
                }
                """),
            ("01-typenames",
                """
                query Typenames {
                  account: node(id: "a1") {
                    __typename
                  }
                  chat: node(id: "c1") {
                    __typename
                  }
                }
                """),
            ("02-member-fields",
                """
                query MemberFields {
                  account: node(id: "a1") {
                    ... on Account {
                      id
                      username
                    }
                  }
                  chat: node(id: "c1") {
                    ... on Chat {
                      id
                      text
                    }
                  }
                }
                """),
            ("03-other-member",
                """
                query OtherMember {
                  account: node(id: "a1") {
                    ... on Chat {
                      id
                    }
                  }
                  chat: node(id: "c1") {
                    ... on Account {
                      id
                    }
                  }
                }
                """),
            ("04-other-member-typename",
                """
                query OtherMemberTypename {
                  account: node(id: "a1") {
                    __typename
                    ... on Chat {
                      id
                    }
                  }
                  chat: node(id: "c1") {
                    __typename
                    ... on Account {
                      id
                    }
                  }
                }
                """),
            ("05-aliased",
                """
                query Aliased {
                  account: node(id: "a1") {
                    id
                    ... on Chat {
                      id
                    }
                  }
                  chat: node(id: "c1") {
                    __typename
                    ... on Account {
                      id
                    }
                  }
                }
                """),
            ("10-member-split",
                """
                query MemberSplit {
                  node(id: "a1") {
                    ... on Account {
                      id
                    }
                    ... on Chat {
                      id
                    }
                  }
                }
                """),
            ("20-node-id-chat",
                """
                query NodeIdChat {
                  node(id: "c1") {
                    id
                  }
                }
                """)
        ];

        var responses = new List<(string Name, OperationResult Response)>();

        try
        {
            // act
            foreach (var (name, operation) in operations)
            {
                using var result = await client.PostAsync(
                    new OperationRequest(operation),
                    new Uri("http://localhost:5000/graphql"),
                    TestContext.Current.CancellationToken);
                responses.Add((name, await result.ReadAsResultAsync(TestContext.Current.CancellationToken)));
            }

            // assert
            var snapshot = Snapshot.Create();

            foreach (var (name, response) in responses)
            {
                snapshot.Add(response, name);
            }

            snapshot.MatchMarkdownSnapshot();
        }
        finally
        {
            foreach (var (_, response) in responses)
            {
                response.Dispose();
            }
        }
    }

    // a owns Account and declares Chat with an @external key.
    public static class NodeLookupA
    {
        public sealed class Query
        {
            [Lookup]
            [Shareable]
            public INode? GetNode([GraphQLType<NonNullType<IdType>>] string id)
                => id switch
                {
                    "a1" => new Account("a1", "a1-username"),
                    "c1" => new Chat("never", "a1"),
                    _ => null
                };
        }

        [InterfaceType("Node")]
        public interface INode
        {
            [GraphQLType<NonNullType<IdType>>]
            string Id { get; }
        }

        [EntityKey("id")]
        public sealed record Account(
            [property: GraphQLType<NonNullType<IdType>>] string Id,
            string Username) : INode;

        [EntityKey("id")]
        public sealed record Chat(
            [property: GraphQLType<NonNullType<IdType>>, ExternalField] string Id,
            [property: GraphQLIgnore] string AccountId) : INode
        {
            public Account GetAccount() => new(AccountId, $"{AccountId}-username");
        }
    }

    // b owns Chat and declares Account with an @external key.
    public static class NodeLookupB
    {
        public sealed class Query
        {
            [Lookup]
            [Shareable]
            public INode? GetNode([GraphQLType<NonNullType<IdType>>] string id)
                => id switch
                {
                    "a1" => new Account("never", "a1"),
                    "c1" => new Chat("c1", "c1-text"),
                    _ => null
                };
        }

        [InterfaceType("Node")]
        public interface INode
        {
            [GraphQLType<NonNullType<IdType>>]
            string Id { get; }
        }

        [EntityKey("id")]
        public sealed record Account(
            [property: GraphQLType<NonNullType<IdType>>, ExternalField] string Id,
            [property: GraphQLIgnore] string Owner) : INode
        {
            public IEnumerable<Chat> GetChats() => Owner == "a1" ? [new Chat("c1", "c1-text")] : [];
        }

        [EntityKey("id")]
        public sealed record Chat(
            [property: GraphQLType<NonNullType<IdType>>] string Id,
            string Text) : INode;
    }

    [AttributeUsage(AttributeTargets.Property)]
    private sealed class ExternalFieldAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
            => descriptor.Directive(External.Instance);
    }
}
