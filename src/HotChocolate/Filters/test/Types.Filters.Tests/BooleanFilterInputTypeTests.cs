using System;
using System.Collections.Generic;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    [Obsolete]
    public class BooleanFilterInputTypeTests
        : TypeTestBase
    {
        [Fact]
        public void Create_Filter_Discover_Everything_Implicitly()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new FooFilterTypeDefaults());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Filter_Discover_Operators_Implicitly()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new FooFilterType());

            // assert
            schema.ToString().MatchSnapshot();
        }

        /// <summary>
        /// This test checks if the binding of all allow methods are correct
        /// </summary>
        [Fact]
        public void Create_Filter_Declare_Operators_Explicitly()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new FilterInputType<Foo>(descriptor =>
            {
                descriptor.Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals().And()
                    .AllowNotEquals();
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Name_Explicitly()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new FilterInputType<Foo>(descriptor =>
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
            ISchema schema = CreateSchema(new FilterInputType<Foo>(descriptor =>
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
            ISchema schema = CreateSchema(builder =>
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
            ISchema schema = CreateSchema(builder =>
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
            ISchema schema = CreateSchema(builder =>
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
            ISchema schema = CreateSchema(builder =>
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
            ISchema schema = CreateSchema(
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

        [Fact]
        public void Bind_Filter_FilterDescirptor_Override()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new FilterInputType<Foo>(descriptor =>
                {
                    descriptor
                        .BindFieldsExplicitly()
                        .Filter(x => x.Bar)
                        .BindFiltersImplicitly();
                    descriptor
                        .BindFieldsExplicitly()
                        .Filter(x => x.Bar)
                        .BindFiltersExplicitly().AllowNotEquals();
                }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Bind_Filter_FilterDescirptor_OverrideFieldDescriptor()
        {
            // arrange
            // act
            IBooleanFilterFieldDescriptor first = null;
            IBooleanFilterFieldDescriptor second = null;
            ISchema schema = CreateSchema(
                new FilterInputType<Foo>(descriptor =>
                {
                    first = descriptor
                        .BindFieldsExplicitly()
                        .Filter(x => x.Bar)
                        .BindFiltersExplicitly()
                        .AllowEquals().Name("this_should_not_be_visible").And()
                        .AllowNotEquals().Name("this_should_not_be_visible_not").And();
                    second = descriptor
                        .BindFieldsExplicitly()
                        .Filter(x => x.Bar)
                        .BindFiltersExplicitly()
                        .AllowEquals().Name("equals").And()
                        .AllowNotEquals().Name("not_equals").And();
                }));

            // assert
            schema.ToString().MatchSnapshot();
            Assert.Equal(first, second);
        }

        [Fact]
        public void Bind_Filter_FilterDescirptor_FirstAddThenIgnore()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new FilterInputType<Foo>(descriptor =>
                {
                    descriptor
                        .BindFieldsExplicitly()
                        .Filter(x => x.Bar)
                        .BindFiltersExplicitly().AllowNotEquals();
                    descriptor.Ignore(x => x.Bar);
                }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Bind_Filter_FilterDescirptor_FirstIgnoreThenAdd()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new FilterInputType<Foo>(descriptor =>
                {
                    descriptor.Ignore(x => x.Bar);
                    descriptor
                        .BindFieldsExplicitly()
                        .Filter(x => x.Bar)
                        .BindFiltersExplicitly().AllowNotEquals();
                }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Ignore_Field_Fields()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
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
            ISchema schema = CreateSchema(
                new FilterInputType<Foo>(d => d
                    .Filter(f => f.Bar)
                    .Ignore()));

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class Foo
        {
            public bool Bar { get; set; }
            public bool? BarNullable { get; set; }
        }

        public class Bar
        {
            public string Qux { get; set; }
        }

        public class FooArray
        {
            public List<bool> BarList { get; set; }
            public IEnumerable<bool> BarEnumerable { get; set; }
            public bool[] BarArray { get; set; }
            public List<bool?> BarListNullable { get; set; }
            public IEnumerable<bool?> BarEnumerableNullable { get; set; }
            public bool?[] BarArrayNullable { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Filter(x => x.Bar);
            }
        }

        public class FooFilterTypeDefaults
            : FilterInputType<Foo>
        {
        }
    }
}
