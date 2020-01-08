using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Filters.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterConventionTests
        : TypeTestBase
    {
        [Fact]
        public void Default_Convention()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new FilterInputType<Foo>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_PascalCase()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddConvention<IFilterConvention>(
                        new FilterConvention(x => x.UsePascalCase())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_SnakeCase()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddConvention<IFilterConvention>(
                        new FilterConvention(x => x.UseSnakeCase())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_Custom()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(x =>
                x.AddConvention<IFilterConvention, CustomConvention>()
                .AddObjectType(x => x.Name("Test")
                .Field("foo")
                .Type<NonNullType<ListType<NonNullType<ObjectType<Foo>>>>>()
                .UseFiltering<FilterInputType<Foo>>())
            );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_ArgumentName()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddConvention<IFilterConvention>(
                        new FilterConvention(x => x.ArgumentName("Test"))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_ElementName()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddConvention<IFilterConvention>(
                        new FilterConvention(x => x.ElementName("item"))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_FilterTypeName()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddConvention<IFilterConvention>(
                        new FilterConvention(
                            x => x.FilterTypeName(
                                (x, type) => x.Naming.GetTypeName(type) + "Test")));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_Root_IgnoreArrayFilterKind()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddConvention<IFilterConvention>(
                        new FilterConvention(
                            x => x.Ignore(FilterKind.Array))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_Root_IgnoreArrayFilterKindThenUnignore()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddConvention<IFilterConvention>(
                        new FilterConvention(
                            x => x.Ignore(FilterKind.Array).Ignore(FilterKind.Array, false))));

            // assert
            schema.ToString().MatchSnapshot();
        }


        //TODO: wire up ignore
        [Fact]
        public void Convention_Root_Operation()
        {
            // arrange
            int propertyCount = 0;
            // act
            ISchema schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddConvention<IFilterConvention>(
                        new FilterConvention(
                            x => x.Operation(FilterOperationKind.Equals).Ignore())));

            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void Convention_Type_Root_IgnoreArrayFilterKind()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddConvention<IFilterConvention>(
                        new FilterConvention(
                            x => x.Type(FilterKind.Array))));

            // assert
            schema.ToString().MatchSnapshot();
        }


        private class CustomConvention : FilterConvention
        {
            protected override void Configure(IFilterConventionDescriptor descriptor)
            {
                base.Configure(descriptor);
                descriptor.ArgumentName("test")
                    .ElementName("TESTelement")
                    .FilterTypeName(
                        (IDescriptorContext context, Type entityType) =>
                            context.Naming.GetTypeName(entityType, TypeKind.Object)
                                + "FilterTest");
            }
        }

        public class Foo
        {
            public short Comparable { get; set; }
            public IEnumerable<short> ComparableEnumerable { get; set; }
            public bool Bool { get; set; }
            public FooBar Object { get; set; }
        }
        public class FooBar
        {
            public string Nested { get; set; }
        }
    }
}
