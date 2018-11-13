using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using Moq;
using Xunit;

namespace HotChocolate.Execution
{
    public class OperationRequestTests
    {
        [Fact]
        public async Task ResolveSimpleOneLevelQuery()
        {
            // arrange
            Schema schema = CreateSchema();

            DocumentNode query = Parser.Default.Parse(@"
                {
                    a
                }");

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().FirstOrDefault();

            // act
            var operationExecuter =
                new OperationExecuter(schema, query, operation);

            IExecutionResult result = await operationExecuter.ExecuteAsync(
                new OperationRequest(schema.Services, CreateSession()),
                CancellationToken.None);

            // assert
            Assert.NotNull(result);
            Assert.Null(result.Errors);
            Assert.Collection(((QueryResult)result).Data,
                item =>
                {
                    Assert.Equal("a", item.Key);
                    Assert.Equal("hello world", item.Value);
                });
        }

        [Fact]
        public async Task ExecuteMutationSerially()
        {
            // arrange
            var state = 0;

            var schema = Schema.Create(
                FileResource.Open("MutationExecutionSchema.graphql"),
                cnf =>
                {
                    cnf.BindResolver(() => state)
                        .To("Query", "state");
                    cnf.BindResolver(() => state)
                        .To("CurrentState", "theNumber");
                    cnf.BindResolver(ctx => state = ctx.Argument<int>("newNumber"))
                        .To("Mutation", "changeTheNumber");
                });

            DocumentNode query = Parser.Default.Parse(
                FileResource.Open("MutationExecutionQuery.graphql"));

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().FirstOrDefault();

            // act
            var operationExecuter =
                new OperationExecuter(schema, query, operation);

            IExecutionResult result = await operationExecuter.ExecuteAsync(
                new OperationRequest(schema.Services, CreateSession()),
                CancellationToken.None);

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        private Schema CreateSchema()
        {
            return Schema.Create(@"
                type Query { a: String }
                ", c =>
            {
                c.BindResolver(() => "hello world")
                    .To("Query", "a");
            });
        }

        private ISession CreateSession()
        {
            var resolverCache = new Mock<IResolverCache>();

            var customContexts = new Mock<ICustomContextProvider>();
            customContexts.Setup(t => t.GetCustomContext<IResolverCache>())
                          .Returns(resolverCache.Object);

            var session = new Mock<ISession>(MockBehavior.Strict);
            session.Setup(t => t.DataLoaders)
                   .Returns((IDataLoaderProvider)null);
            session.Setup(t => t.CustomContexts)
                   .Returns(customContexts.Object);

            return session.Object;
        }
    }
}
