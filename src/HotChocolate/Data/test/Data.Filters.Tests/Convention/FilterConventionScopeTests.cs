using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters;

public class FilterConventionScopeTests
{
    [Fact]
    public void FilterConvention_Should_Work_When_ConfiguredWithAttributes()
    {
        // arrange
        // act
        ISchema schema = SchemaBuilder.New()
            .AddConvention<IFilterConvention, BarFilterConvention>("Bar")
            .AddQueryType<Query1>()
            .AddFiltering()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void FilterConvention_Should_Work_When_ConfiguredWithType()
    {
        // arrange
        // act
        ISchema schema = SchemaBuilder.New()
            .AddConvention<IFilterConvention, BarFilterConvention>("Bar")
            .AddQueryType<QueryType>()
            .AddFiltering()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field("foos")
                .Resolve(Array.Empty<Foo>().AsQueryable())
                .UseFiltering();

            descriptor.Field("foosBar")
                .Resolve(Array.Empty<Foo>().AsQueryable())
                .UseFiltering("Bar");
        }
    }

    public class Query1
    {
        [UseFiltering]
        public IQueryable<Foo> Foos() => Array.Empty<Foo>().AsQueryable();

        [UseFiltering(Scope = "Bar")]
        public IQueryable<Foo> FoosBar() => Array.Empty<Foo>().AsQueryable();
    }

    public class BarFilterConvention : FilterConvention
    {
        protected override void Configure(IFilterConventionDescriptor descriptor)
        {
            descriptor.AddDefaults();
            descriptor.Operation(DefaultFilterOperations.Equals).Name("EQUALS");
        }
    }

    public class Foo
    {
        public string Bar { get; set; } = default!;
    }
}
