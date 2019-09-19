using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
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
                    .NodeResolver((ctx, id) =>
                        Task.FromResult(new Bar { Id = id })))
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ bar { id } }");

            // assert
            result.MatchSnapshot();
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
                    .NodeResolver((ctx, id) =>
                        Task.FromResult(new Bar { Id = id })))
                .Use(next => async ctx =>
                {
                    await next(ctx);

                    if (ctx.LocalContextData.TryGetValue(
                        WellKnownContextData.Type,
                        out object value))
                    {
                        type = (NameString)value;
                    }
                })
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IExecutionResult result = await executor.ExecuteAsync(
                "{ node(id: \"QmFyCmQxMjM=\") { id } }");

            Assert.Equal("Bar", type);
        }


        public class Foo
        {
            public Bar Bar { get; set; } = new Bar { Id = "123" };
        }

        public class Bar
        {
            public string Id { get; set; }
        }
    }
}
