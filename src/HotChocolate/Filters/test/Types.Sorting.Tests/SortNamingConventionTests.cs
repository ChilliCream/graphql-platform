using System;
using HotChocolate.Types.Descriptors;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    [Obsolete]
    public class SortingNamingConventionTests
        : TypeTestBase
    {

        [Fact]
        public void Default_Convention()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(x => x.AddSortingType());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_PascalCase()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(x =>
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
            ISchema schema = CreateSchema(x =>
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
            ISchema schema = CreateSchema(x =>
                x.AddConvention<ISortingNamingConvention, CustomConvention>()
                .AddSortingType()
            );

            // assert
            schema.ToString().MatchSnapshot();
        }

        private class CustomConvention : SortingNamingConventionSnakeCase
        {
            public override NameString ArgumentName => "test";

            public override NameString SortKindAscName => "TESTASC";

            public override NameString SortKindDescName => "TESTDESC";

            public override NameString GetSortingTypeName(
                IDescriptorContext context,
                Type entityType)
            {
                return base.GetSortingTypeName(context, entityType).Add("Test");
            }

            public override NameString GetSortingOperationKindTypeName(
                IDescriptorContext context,
                Type entityType)
            {
                return base.GetSortingOperationKindTypeName(context, entityType).Add("Test");
            }
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
