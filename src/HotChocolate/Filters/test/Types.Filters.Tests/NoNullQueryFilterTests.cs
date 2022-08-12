using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Types.Filters;

[Obsolete]
public class NoNullQueryFilterTests
{
    [Fact]
    public void Create_Schema()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Execute_Filter()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = executor.Execute(
            "{ foos(where: { bar_starts_with: \"a\" }) { bar } }");

        // assert
        result.MatchSnapshot();
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.Foos).UseFiltering();
        }
    }

    public class Query
    {
        public IEnumerable<Foo> Foos { get; } = new[]
        {
            new Foo {Bar = "aa", Baz = "bb"},
            new Foo {Bar = "cc", Baz = "dd"},
            new Foo {Bar = "ee", Baz = "ff"},
            new Foo {Bar = "gg", Baz = "hh"},
        };
    }

    public class Foo
    {
        public string? Bar { get; set; }

        public string Baz { get; set; } = string.Empty;
    }
}