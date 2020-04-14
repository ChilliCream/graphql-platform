using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Sorting.Conventions;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    public class SortingConventionTests
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
                x.AddConvention<ISortingConvention>(new SortingConvention(x => x.UsePascalCase()))
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
                x.AddConvention<ISortingConvention>(new SortingConvention(x => x.UseSnakeCase()))
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
                x.AddConvention<ISortingConvention>(new CustomConvention())
                .AddSortingType()
            );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_AddTryCreateImplicitSorting()
        {
            // arrange
            var hasBeenCalled = false;
            var convention = new SortingConvention(
                x => x.AddImplicitSorting(
                        (IDescriptorContext _,
                        Type __,
                        PropertyInfo ___,
                        ISortingConvention ____,
                        out SortOperationDefintion definition)
                        =>
                        {
                            hasBeenCalled = true;
                            definition = null;
                            return false;
                        }, 0));

            // act
            ISchema schema = CreateSchema(x =>
                x.AddConvention<ISortingConvention>(convention)
                .AddSortingType()
            );

            // assert
            Assert.True(hasBeenCalled, "Implicit Sorting should have been called!");
        }

        private class CustomConvention
            : SortingConvention
        {
            protected override void Configure(ISortingConventionDescriptor descriptor)
                => descriptor.ArgumentName("test")
                        .AscendingName("TESTASC")
                        .DescendingName("TESTDESC")
                        .TypeName(GetSortingTypeName)
                        .OperationKindTypeName(GetSortingOperationKindTypeName)
                        .Description(GetDescriptionFactory);

            private string GetDescriptionFactory(
                IDescriptorContext context,
                Type entityType)
                    => "TestDescription";

            private NameString GetSortingTypeName(
                IDescriptorContext context,
                Type entityType)
                    => context.Naming.GetTypeName(entityType, TypeKind.Object) + "SortTest";

            private NameString GetSortingOperationKindTypeName(
                IDescriptorContext context,
                Type entityType)
                    => context.Naming.GetTypeName(entityType, TypeKind.Object) + "Test";
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
