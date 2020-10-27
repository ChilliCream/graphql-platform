using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Stitching.Integration
{
    public class RewriteTypesTests : IClassFixture<StitchingTestContext>
    {
        public RewriteTypesTests(StitchingTestContext context)
        {
            Context = context;
        }

        protected StitchingTestContext Context { get; }

        [Fact]
        public async Task AutoMerge_Schema()
        {
            // arrange
            IHttpClientFactory httpClientFactory =
                Context.CreateDefaultRemoteSchemas();

            // act
            IServiceProvider services =
                new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL()
                    .AddRemoteSchemaFromString(
                        "AdvisorClient",
                        FileResource.Open("AdvisorClient.graphql"))
                    .AddRemoteSchemaFromString(
                        "ContractClient",
                        FileResource.Open("ContractClient.graphql"))
                    .AddRemoteSchemaFromString(
                        "DocumentClient",
                        FileResource.Open("DocumentClient.graphql"))
                    .AddType(new IntType("PaginationAmount"))
                    .AddType(new IntType())
                    .AddMergedDocumentRewriter(d =>
                    {
                        var rewriter = new DocumentRewriter();
                        return (DocumentNode)rewriter.Rewrite(d, null);
                    })
                    .RewriteType("ContractClient", "Int", "PaginationAmount")
                    .AddGraphQL("AdvisorClient")
                    .AddType(new IntType("PaginationAmount"))
                    .AddType(new IntType())
                    .AddGraphQL("ContractClient")
                    .AddType(new IntType("PaginationAmount"))
                    .AddType(new IntType())
                    .AddGraphQL("DocumentClient")
                    .AddType(new IntType("PaginationAmount"))
                    .AddType(new IntType())
                    .Services
                    .BuildServiceProvider();

            // assert
            IRequestExecutor contractExecutor =
                await services.GetRequestExecutorAsync("ContractClient");

            ObjectType type = contractExecutor.Schema.GetType<ObjectType>("ZmaContract");

            Assert.Equal(
                "Int",
                type.Fields["accountTransactions"].Arguments["first"].Type.NamedType().Name.Value);

            IRequestExecutor executor =
                await services.GetRequestExecutorAsync();

            type = executor.Schema.GetType<ObjectType>("ZmaContract");

            Assert.Equal(
                "PaginationAmount",
                type.Fields["accountTransactions"].Arguments["first"].Type.NamedType().Name.Value);

            Assert.True(executor.Schema.TryGetDirectiveType("translatable", out _));
        }

        private class DocumentRewriter : SchemaSyntaxRewriter<object>
        {
            protected override FieldDefinitionNode RewriteFieldDefinition(
                FieldDefinitionNode node,
                object context)
            {
                if(node.Type.NamedType().Name.Value.EndsWith("Connection") &&
                    node.Arguments.Any(t => t.Name.Value.EqualsOrdinal("first") &&
                    t.Type.NamedType().Name.Value.EqualsOrdinal("Int")))
                {
                    var arguments = node.Arguments.ToList();

                    InputValueDefinitionNode first =
                        arguments.First(t => t.Name.Value.EqualsOrdinal("first"));

                    InputValueDefinitionNode last =
                        arguments.First(t => t.Name.Value.EqualsOrdinal("last"));

                    arguments[arguments.IndexOf(first)] =
                        first.WithType(RewriteType(first.Type, "PaginationAmount"));

                    arguments[arguments.IndexOf(last)] =
                        last.WithType(RewriteType(first.Type, "PaginationAmount"));

                    node = node.WithArguments(arguments);
                }

                return base.RewriteFieldDefinition(node, context);
            }

            private static ITypeNode RewriteType(ITypeNode type, NameString name)
            {
                if (type is NonNullTypeNode nonNullType)
                {
                    return new NonNullTypeNode(
                        (INullableTypeNode)RewriteType(nonNullType.Type, name));
                }

                if (type is ListTypeNode listType)
                {
                    return new ListTypeNode(RewriteType(listType.Type, name));
                }

                return new NamedTypeNode(name);
            }
        }
    }
}
