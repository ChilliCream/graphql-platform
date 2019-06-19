using System.Collections.Generic;
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
                new Foo { Bar = "ac" },
                new Foo { Bar = "ab" },
                new Foo { Bar = "ac" },
            };
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

    }
}
