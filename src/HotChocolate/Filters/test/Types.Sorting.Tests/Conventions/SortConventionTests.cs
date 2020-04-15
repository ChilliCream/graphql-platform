using System;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Sorting.Conventions;
using HotChocolate.Types.Sorting.Expressions;
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

        [Fact]
        public void Convention_OverrideCompiler()
        {
            // arrange
            var hasBeenCalled = false;
            var convention = new SortingConvention(
                x => x.UseExpressionVisitor()
                        .UseDefault()
                        .Compile((visitorDefinition, context, source) =>
                        {
                            hasBeenCalled = true;
                            return SortCompilerDefault.Compile(visitorDefinition, context, source);
                        }));

            // act
            ISchema schema = CreateSchemaWithRootSort(x =>
                x.AddConvention<ISortingConvention>(convention)
                .AddSortingType()
            );

            schema.MakeExecutable().ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{foo(order_by:{comparable:DESC}) {comparable}}")
                .Create());

            // assert
            Assert.True(hasBeenCalled, "Compiler should have been called!");
        }

        [Fact]
        public void Convention_OverrideCreateOperation()
        {
            // arrange
            var hasBeenCalled = false;
            var convention = new SortingConvention(
                x => x.UseExpressionVisitor()
                        .UseDefault()
                        .CreateOperation((visitorDefinition, context, kind) =>
                        {
                            hasBeenCalled = true;
                            return CreateSortOperationDefault.CreateSortOperation(
                                visitorDefinition, context, kind);
                        }));

            // act
            ISchema schema = CreateSchemaWithRootSort(x =>
                x.AddConvention<ISortingConvention>(convention)
            );

            schema.MakeExecutable().ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{foo(order_by:{comparable:DESC}) {comparable}}")
                .Create());

            // assert
            Assert.True(hasBeenCalled, "CreateSortOperation should have been called!");
        }

        public static ISchema CreateSchemaWithRootSort(
            Action<ISchemaBuilder> configure)
        {
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                .Resolver(Array.Empty<Foo>())
                .Type<NonNullType<ListType<NonNullType<ObjectType<Foo>>>>>()
                .UseSorting());

            configure(builder);

            return builder.Create();
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
