using System.Collections.Generic;
using HotChocolate.Execution;
using NetTopologySuite.Geometries;
using Snapshooter.Xunit;
using HotChocolate.Types.Filters.Conventions;
using Xunit;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Types.Spatial.Filters.Expressions;

namespace HotChocolate.Types.Filters
{
    public class GeometryFilterTests
    {
        [Fact]
        public void Create_Schema_With_FilterType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseSpatialFilters()))
                .AddSpatialTypes()
                .AddQueryType<QueryType>().Create();

            // assert
            schema.ToString().MatchSnapshot();
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
                new Foo { Bar = new Point(new Coordinate(1,1)) },
                new Foo { Bar = new Point(new Coordinate(10,10)) },
                new Foo { Bar = new Point(new Coordinate(100,100)) },
                new Foo { Bar = new Point(new Coordinate(1000,1000)) },
                new Foo { Bar = new Point(new Coordinate(10000,10000)) },
                new Foo { Bar = new Point(new Coordinate(100000,100000)) },
            };
        }

        public class Foo
        {
            public Point Bar { get; set; }
        }
    }
}
