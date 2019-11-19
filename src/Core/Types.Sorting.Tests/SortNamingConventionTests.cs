using System.Collections.Generic;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    public class SortingNamingConventionTests
        : TypeTestBase
    {

        [Fact]
        public void Default_Convention()
        {
            // arrange
            // act
            var schema = CreateSchema(x =>
                x.AddSortingType()
            );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_PascalCase()
        {
            // arrange
            // act
            var schema = CreateSchema(x =>
                x.AddConvention<ISortingNamingConvention, SortingNamingConventionPascalCase>()
                .AddSortingType()
            );

            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void Convention_SnakeCase()
        {
            // arrange
            // act
            var schema = CreateSchema(x =>
                x.AddConvention<ISortingNamingConvention, SortingNamingConventionSnakeCase>()
                .AddSortingType()
            );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_Custom()
        {
            // arrange
            // act
            var schema = CreateSchema(x =>
                x.AddConvention<ISortingNamingConvention, CustomConvention>()
                .AddSortingType()
            );

            // assert
            schema.ToString().MatchSnapshot();
        }

        private class CustomConvention : ISortingNamingConvention
        {
            public NameString ArgumentName => "test";

            public NameString SortKindAscName => "TESTASC";

            public NameString SortKindDescName => "TESTDESC";
        }

    }

    internal class Foo
    {
        public short Comparable { get; set; }
    }

    internal static class TestExtension
    {
        public static ISchemaBuilder AddSortingType(this ISchemaBuilder ctx, Foo[] resolvedItems = null)
        {
            ctx.AddObjectType(x => x.Name("Test")
                .Field("foo")
                .Resolver(resolvedItems)
                .Type<NonNullType<ListType<NonNullType<ObjectType<Foo>>>>>()
                .UseSorting());
            return ctx;
        }
    }
}
