using System;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterInputTypeTests
        : TypeTestBase
    {
        [Fact]
        public void Create_Implicit_Filters()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Explicit_Filters()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(d => d
                    .Filter(f => f.Bar)
                        .BindFilters(BindingBehavior.Explicit)
                        .AllowEquals()
                        .Name("foo_eq")));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Rename_Specific_Filter()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(d => d
                    .Filter(f => f.Bar)
                        .AllowEquals()
                        .Name("foo")));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Define_Filters_By_Configure_Override()
        {
            // arrange
            // act
            var schema = CreateSchema(new FooFilterType());

            // assert
            schema.ToString().MatchSnapshot();
        }


        public class Foo
        {
            public string Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Filter(t => t.Bar)
                    .BindExplicitly()
                    .AllowContains().And()
                    .AllowEquals().Name("equals").And()
                    .AllowIn();
            }
        }
    }
}
