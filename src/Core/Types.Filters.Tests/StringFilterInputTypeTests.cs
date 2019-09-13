using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class StringFilterInputTypeTests
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
                        .BindFiltersExplicitly()
                        .AllowEquals()
                        .Name("foo_eq")));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Explicit_Filters_All_Operations()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(d => d
                    .Filter(f => f.Bar)
                        .BindFiltersExplicitly()
                        .AllowEquals()
                        .And().AllowContains()
                        .And().AllowEndsWith()
                        .And().AllowEquals()
                        .And().AllowIn()
                        .And().AllowNotContains()
                        .And().AllowNotEndsWith()
                        .And().AllowNotEquals()
                        .And().AllowNotIn()
                        .And().AllowNotStartsWith()
                        .And().AllowStartsWith()));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Ignore_Field_Fields()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(d => d
                    .Ignore(f => f.Bar)));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Ignore_Field_2()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(d => d
                    .Filter(f => f.Bar)
                    .Ignore()));

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

        [Fact]
        public void Declare_Name_Explicitly()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>(descriptor =>
            {
                descriptor.Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals()
                    .Name("custom_equals");
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Description_Explicitly()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>(descriptor =>
            {
                descriptor.Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals()
                    .Description("custom_equals_description");
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Directive_By_Name()
        {
            // arrange
            // act
            var schema = CreateSchema(builder =>
                builder.AddType(new FilterInputType<Foo>(d =>
                {
                    d.Filter(x => x.Bar)
                        .BindFiltersExplicitly()
                        .AllowEquals()
                        .Directive("bar");
                }))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.InputFieldDefinition))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Directive_By_Name_With_Argument()
        {
            // arrange
            // act
            var schema = CreateSchema(builder =>
                builder.AddType(new FilterInputType<Foo>(d =>
                {
                    d.Filter(x => x.Bar)
                        .BindFiltersExplicitly()
                        .AllowEquals()
                        .Directive("bar",
                            new ArgumentNode("qux",
                                new StringValueNode("foo")));
                }))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.InputFieldDefinition)
                    .Argument("qux")
                    .Type<StringType>())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Directive_With_Clr_Type()
        {
            // arrange
            // act
            var schema = CreateSchema(builder =>
                builder.AddType(new FilterInputType<Foo>(d =>
                {
                    d.Filter(x => x.Bar)
                        .BindFiltersExplicitly()
                        .AllowEquals()
                        .Directive<Bar>();
                }))
                .AddDirectiveType(new DirectiveType<Bar>(d => d
                    .Location(DirectiveLocation.InputFieldDefinition))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Directive_With_Clr_Instance()
        {
            // arrange
            // act
            var schema = CreateSchema(builder =>
                builder.AddType(new FilterInputType<Foo>(d =>
                {
                    d.Filter(x => x.Bar)
                        .BindFiltersExplicitly()
                        .AllowEquals()
                        .Directive(new Bar());
                }))
                .AddDirectiveType(new DirectiveType<Bar>(d => d
                    .Location(DirectiveLocation.InputFieldDefinition))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Bind_Filter_Implicitly()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(descriptor =>
                {
                    descriptor
                        .BindFieldsExplicitly()
                        .Filter(x => x.Bar)
                        .BindFiltersExplicitly()
                        .BindFiltersImplicitly();
                }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class Bar
        {
            public string Baz { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Filter(t => t.Bar)
                    .BindFiltersExplicitly()
                    .AllowContains().And()
                    .AllowEquals().Name("equals").And()
                    .AllowIn();
            }
        }
    }
}
