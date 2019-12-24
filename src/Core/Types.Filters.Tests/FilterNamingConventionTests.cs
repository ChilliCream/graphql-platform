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
    public class FilterNamingConventionTests
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

        private class CustomConvention : FilterConvention
        {
            protected override void Configure(IFilterConventionDescriptor descriptor)
            {
                base.Configure(descriptor);
                descriptor.ArgumentName("test")
                    .ArrayFilterPropertyName("TESTelement")
                    .GetFilterTypeName(
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
