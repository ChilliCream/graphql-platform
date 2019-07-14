using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;
using HotChocolate.AspNetCore;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Moq;
using Microsoft.AspNetCore.TestHost;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Delegation;
using System.Linq;
using Snapshooter;
using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.Stitching
{
    public class TypeRewriterTests
        : StitchingTestBase
    {
        public TypeRewriterTests(TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

        [Fact]
        public async Task CustomRewriterTakesPriority()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            var connections = new Dictionary<string, HttpClient>();
            serviceCollection.AddSingleton(CreateRemoteSchemas(connections));

            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("someSchema")
                .AddTypeRewriter(new TypeRewriter()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result = null;

            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                    .SetQuery("{ foo }")
                    .AddProperty("foo_a", "bar")
                    .SetServices(scope.ServiceProvider)
                    .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot(new SnapshotNameExtension("result"));
            executor.Schema.ToString().MatchSnapshot(
                new SnapshotNameExtension("schema"));
        }
        protected override IHttpClientFactory CreateRemoteSchemas(
               Dictionary<string, HttpClient> connections)
        {
            TestServer server = TestServerFactory.Create(
                services => services.AddGraphQL(
                    SchemaBuilder.New()
                        .AddDocumentFromString(
                            "type Query { foo(a: String): String }")
                        .AddResolver("Query", "foo", c => c.Argument<string>("a"))
                        .Create()),
                app => app.UseGraphQL());

            connections["someSchema"] = server.CreateClient();

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(t => t.CreateClient(It.IsAny<string>()))
                .Returns(new Func<string, HttpClient>(n =>
                {
                    if (connections.ContainsKey(n))
                    {
                        return connections[n];
                    }

                    throw new Exception();
                }));
            return httpClientFactory.Object;
        }

        public class TypeRewriter
            : ITypeRewriter
        {
            public ITypeDefinitionNode Rewrite(
                ISchemaInfo schema,
                ITypeDefinitionNode typeDefinition)
            {
                if (typeDefinition.Name.Value.Equals("Query")
                    && typeDefinition is ObjectTypeDefinitionNode objectType)
                {
                    var path = new SelectionPathComponent(
                        new NameNode("foo"),
                        new[]
                        {
                        new ArgumentNode(
                            "a",
                            new ScopedVariableNode(
                                ScopeNames.ContextData,
                                "foo_a"))
                        });

                    Dictionary<string, FieldDefinitionNode> fields =
                        objectType.Fields.ToDictionary(t => t.Name.Value);
                    fields["foo"] = fields["foo"]
                        .WithArguments(Array.Empty<InputValueDefinitionNode>())
                        .AddDelegationPath("someSchema", path);
                    return objectType.WithFields(fields.Values.ToArray());
                }
                return typeDefinition;
            }
        }
    }
}
