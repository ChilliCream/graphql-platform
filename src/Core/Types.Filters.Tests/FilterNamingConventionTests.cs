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
        public void ServiceProvider_PascalCase()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IFilterNamingConvention, FilterNamingConventionPascalCase>();
            // arrange

            // act
            var schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddServices(services.BuildServiceProvider()));

            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void ServiceProvider_SnakeCase()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IFilterNamingConvention, FilterNamingConventionSnakeCase>();
            // arrange

            // act
            var schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddServices(services.BuildServiceProvider()));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ServiceProvider_CamelCase()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IFilterNamingConvention, FilterNamingConventionCamelCase>();
            // arrange

            // act
            var schema = CreateSchema(
                x => x.AddType<FilterInputType<Foo>>()
                    .AddServices(services.BuildServiceProvider()));

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class Foo
        {
            public short Comparable { get; set; }
            public bool Bool { get; set; }
            public FooBar Object { get; set; }
        }
        public class FooBar
        {
            public string Nested { get; set; }
        }
    }
}
