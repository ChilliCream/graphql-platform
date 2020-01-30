using System;
using HotChocolate.Execution;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Integration.EmbeddedResolvers
{
    public class EmbeddedResolverTests
    {
        [Fact]
        public void ResolverResultIsObject()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                QueryRequestBuilder.New()
                    .SetQuery("{ foo { bar { baz }}}")
                    .Create());

            // assert
            result.MatchSnapshot();
        }


        public class QueryType
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field<QueryType>(t => t.GetFoo()).Type<FooType>();
            }

            public object GetFoo()
            {
                return new object();
            }
        }

        public class FooType
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Foo");
                descriptor.Field<FooType>(t => t.Bar()).Type<BarType>();
            }

            public Bar Bar()
            {
                return new Bar();
            }
        }

        public class BarType
            : ObjectType<Bar>
        {
            protected override void Configure(IObjectTypeDescriptor<Bar> descriptor)
            {
            }
        }

        public class Bar
        {
            public string Baz { get; } = "Bar";
        }
    }
}
