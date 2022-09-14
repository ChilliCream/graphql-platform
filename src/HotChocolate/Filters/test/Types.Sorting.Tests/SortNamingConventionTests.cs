using HotChocolate.Types.Descriptors;
using Snapshooter.Xunit;

namespace HotChocolate.Types.Sorting;

[Obsolete]
public class SortingNamingConventionTests : TypeTestBase
{

    [Fact]
    public void Default_Convention()
    {
        // arrange
        // act
        var schema = CreateSchema(x => x.AddSortingType());

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

    private sealed class CustomConvention : SortingNamingConventionSnakeCase
    {
        public override string ArgumentName => "test";

        public override string SortKindAscName => "TESTASC";

        public override string SortKindDescName => "TESTDESC";

        public override string GetSortingTypeName(
            IDescriptorContext context,
            Type entityType)
        {
            return string.Concat(base.GetSortingTypeName(context, entityType), "Test");
        }

        public override string GetSortingOperationKindTypeName(
            IDescriptorContext context,
            Type entityType)
        {
            return string.Concat(base.GetSortingOperationKindTypeName(context, entityType), "Test");
        }
    }
}

internal class Foo
{
    public short Comparable { get; set; }
}

internal static class TestExtension
{
    [Obsolete]
    public static ISchemaBuilder AddSortingType(this ISchemaBuilder ctx, Foo[] resolvedItems = null)
    {
        ctx.AddObjectType(x => x.Name("Test")
            .Field("foo")
            .Resolve(resolvedItems)
            .Type<NonNullType<ListType<NonNullType<ObjectType<Foo>>>>>()
            .UseSorting());
        return ctx;
    }
}
