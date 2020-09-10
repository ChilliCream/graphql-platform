using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;
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
                    .AddConvention<IFilterNamingConvention, FilterNamingConventionPascalCase>());

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
                    .AddConvention<IFilterNamingConvention, FilterNamingConventionSnakeCase>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_Custom()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(x =>
                x.AddConvention<IFilterNamingConvention, CustomConvention>()
                .AddObjectType(x => x.Name("Test")
                .Field("foo") 
                .Type<NonNullType<ListType<NonNullType<ObjectType<Foo>>>>>()
                .UseFiltering<FilterInputType<Foo>>())
            );

            // assert
            schema.ToString().MatchSnapshot();
        }

        private class CustomConvention : FilterNamingConventionSnakeCase
        {
            public override NameString ArgumentName => "test";

            public override NameString ArrayFilterPropertyName => "TESTelement";

            public override NameString GetFilterTypeName(IDescriptorContext context, Type entityType)
            {
                return base.GetFilterTypeName(context, entityType) + "Test";
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
