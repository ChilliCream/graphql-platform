using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class GlobalObjectIdentificationTests : FusionTestBase
{
    [Fact]
    public async Task Test()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddGlobalObjectIdentification()
                // TODO: Remove once proper support has been implemented in HC
                .TryAddTypeInterceptor<NodeFieldLookupTypeInterceptor>()
                .AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddGlobalObjectIdentification()
                // TODO: Remove once proper support has been implemented in HC
                .TryAddTypeInterceptor<NodeFieldLookupTypeInterceptor>()
                .AddQueryType<SourceSchema2.Query>());

        // act
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // assert
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              node(id: "RGlzY3Vzc2lvbjox") {
                ... on Discussion {
                  title
                  commentCount
                }
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    public static class SourceSchema1
    {
        public class Query
        {
            [Lookup]
            public Discussion? GetDiscussionById([Is("id")] [ID] int discussionId)
                => new Discussion(discussionId, "Discussion " + discussionId);
        }

        [Node]
        public record Discussion(int Id, string Title)
        {
            [NodeResolver]
            public static Discussion Get(int id)
                => new Discussion(id, "Discussion " + id);
        }
    }

    public static class SourceSchema2
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Discussion? GetDiscussionById([ID] int id)
                => new Discussion(id, id * 3);
        }

        [Node]
        public record Discussion(int Id, int CommentCount)
        {
            [NodeResolver]
            public static Discussion Get(int id)
                => new Discussion(id, id * 3);
        }
    }

    private sealed class NodeFieldLookupTypeInterceptor : TypeInterceptor
    {
        private ITypeCompletionContext? _queryContext;

        public override void OnAfterResolveRootType(ITypeCompletionContext completionContext,
            ObjectTypeConfiguration configuration,
            OperationType operationType)
        {
            if (operationType is OperationType.Query)
            {
                _queryContext = completionContext;
            }
        }

        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext,
            TypeSystemConfiguration configuration)
        {
            if (ReferenceEquals(_queryContext, completionContext)
                && configuration is ObjectTypeConfiguration objectTypeConfiguration)
            {
                var nodeField = objectTypeConfiguration.Fields.FirstOrDefault(x => x.Name == "node");

                nodeField?.AddDirective(Lookup.Instance, completionContext.TypeInspector);
            }
        }
    }
}
