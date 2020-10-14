using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors.Definitions;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class NodeFieldSupportTests
    {
        [Fact]
        public async Task NodeId_Is_Correctly_Formatted()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .EnableRelaySupport()
                .AddQueryType<Foo>()
                .AddObjectType<Bar>(d => d
                    .AsNode()
                    .IdField(t => t.Id)
                    .NodeResolver((ctx, id) => Task.FromResult(new Bar { Id = id })))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ bar { id } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Node_Type_Is_Correctly_In_Context()
        {
            string type = null;

            ISchema schema = SchemaBuilder.New()
                .EnableRelaySupport()
                .AddQueryType<Foo>()
                .AddObjectType<Bar>(d => d
                    .AsNode()
                    .IdField(t => t.Id)
                    .NodeResolver((ctx, id) => Task.FromResult(new Bar { Id = id })))
                .Use(next => async ctx =>
                {
                    await next(ctx);

                    if (ctx.LocalContextData.TryGetValue(
                        WellKnownContextData.InternalType,
                        out object value))
                    {
                        type = (NameString)value;
                    }
                })
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            IExecutionResult result = await executor.ExecuteAsync(
                "{ node(id: \"QmFyCmQxMjM=\") { id } }");

            Assert.Equal("Bar", type);
        }

        [Fact]
        public async Task Node_Resolve_Separated_Resolver()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .EnableRelaySupport()
                .AddQueryType<Foo>()
                .AddObjectType<Bar>(d => d
                    .ImplementsNode()
                    .IdField(t => t.Id)
                    .ResolveNodeWith<BarResolver>(t => t.GetBarAsync(default)))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ node(id: \"QmFyCmQxMjM=\") { id } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Node_Resolve_Separated_Resolver_ImplicitId()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .EnableRelaySupport()
                .AddQueryType<Foo>()
                .AddObjectType<Bar>(d => d
                    .ImplementsNode()
                    .ResolveNodeWith<BarResolver>(t => t.GetBarAsync(default)))
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ node(id: \"QmFyCmQxMjM=\") { id } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        public class Foo
        {
            public Bar Bar { get; set; } = new Bar { Id = "123" };
        }

        public class Bar
        {
            public string Id { get; set; }
        }

        public class BarResolver
        {
            public Bar GetBarAsync(string id) => new Bar { Id = id };
        }
    }
}
