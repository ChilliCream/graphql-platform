using System;
using System.Collections.Generic;
using System.Text;
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
            var schema = CreateSchema(new FilterInputType<Foo>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convention_PascalCase()
        {
            // arrange
            // act
            var schema = CreateSchema(
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
            var schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddConvention<IFilterNamingConvention, FilterNamingConventionSnakeCase>());

            // assert
            schema.ToString().MatchSnapshot();
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
