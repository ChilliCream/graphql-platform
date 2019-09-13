using System.Collections.Generic;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterTests
    {
        [Fact]
        public void Create_Schema_With_FilteType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Schema_With_FilteType_With_Fluent_API()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(d =>
                    d.Field(m => m.Foos)
                        .UseFiltering<Foo>(f =>
                            f.BindFieldsExplicitly()
                                .Filter(m => m.Bar)
                                .BindFiltersExplicitly()
                                .AllowEquals()))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_starts_with: \"a\" }) { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_With_Variables()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    @"query filter($a: String) {
                        foos(where: { bar_starts_with: $a }) {
                            bar
                        }
                    }")
                .SetVariableValue("a", "a")
                .Create();

            // act
            IExecutionResult result = executor.Execute(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_As_Variable()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    @"query filter($a: FooFilter) {
                        foos(where: $a) {
                            bar
                        }
                    }")
                .SetVariableValue("a", new Dictionary<string, object>
                {
                    { "bar_starts_with", "a" }
                })
                .Create();

            // act
            IExecutionResult result = executor.Execute(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Is_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Infer_Filter_From_Field()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(d => d.Field(t => t.Foos).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_starts_with: \"a\" }) { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Equals_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(d => d.Field(t => t.Foos).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar: null }) { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Not_Equals_Null()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(d => d.Field(t => t.Foos).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_not: null }) { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_In()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(d => d.Field(t => t.Foos).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_in: [ \"aa\" \"ab\" ] }) { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Equals_And()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(d => d.Field(t => t.Foos).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { AND: [ { bar: \"aa\" } { bar: \"ba\" } ] })" +
                " { bar } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Equals_Or()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(d => d.Field(t => t.Foos).UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { OR: [ { bar: \"aa\" } { bar: \"ba\" } ] })" +
                " { bar } }");

            // assert
            result.MatchSnapshot();
        }

        public class QueryType
            : ObjectType<Query>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor.Field(t => t.Foos).UseFiltering<Foo>();
            }
        }

        public class Query
        {
            public IEnumerable<Foo> Foos { get; } = new[]
            {
                new Foo { Bar = "aa" },
                new Foo { Bar = "ba" },
                new Foo { Bar = "ca" },
                new Foo { Bar = "ab" },
                new Foo { Bar = "ac" },
                new Foo { Bar = "ad" },
                new Foo { Bar = null }
            };
        }

        public class Foo
        {
            public string Bar { get; set; }
        }
    }
}
