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
                .AddQueryType<Query>(d => d.Field(t => t.Foos).UseFilter())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_starts_with: \"a\" }) { bar } }");

            // assert
            result.MatchSnapshot();
        }


        public class QueryType
            : ObjectType<Query>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor.Field(t => t.Foos).UseFilter<Foo>();
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
            };
        }

        public class Foo
        {
            public string Bar { get; set; }
        }
    }
}
