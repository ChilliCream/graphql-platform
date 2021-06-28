using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    [Obsolete]
    public class ComparableFilterInputTypeTests
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

        [Fact]
        public void Bind_Filter_FilterDescirptor_OverrideFieldDescriptor()
        {
            // arrange
            // act
            IComparableFilterFieldDescriptor first = null;
            IComparableFilterFieldDescriptor second = null;
            ISchema schema = CreateSchema(
                new FilterInputType<Foo>(descriptor =>
                {
                    first = descriptor
                        .BindFieldsExplicitly()
                        .Filter(x => x.BarShort)
                        .BindFiltersExplicitly()
                        .AllowEquals().Name("this_should_not_be_visible").And()
                        .AllowNotEquals().Name("this_should_not_be_visible").And()
                        .AllowIn().Name("this_should_not_be_visible").And()
                        .AllowNotIn().Name("this_should_not_be_visible").And()
                        .AllowGreaterThan().Name("this_should_not_be_visible").And()
                        .AllowNotGreaterThan().Name("this_should_not_be_visible").And()
                        .AllowGreaterThanOrEquals().Name("this_should_not_be_visible").And()
                        .AllowNotGreaterThanOrEquals().Name("this_should_not_be_visible").And()
                        .AllowLowerThan().Name("this_should_not_be_visible").And()
                        .AllowNotLowerThan().Name("this_should_not_be_visible").And()
                        .AllowLowerThanOrEquals().Name("this_should_not_be_visible").And()
                        .AllowNotLowerThanOrEquals().Name("this_should_not_be_visible").And();
                    second = descriptor
                        .BindFieldsExplicitly()
                        .Filter(x => x.BarShort)
                        .AllowEquals().Name("eq").And()
                        .AllowNotEquals().Name("neq").And()
                        .AllowIn().Name("in").And()
                        .AllowNotIn().Name("nin").And()
                        .AllowGreaterThan().Name("gt").And()
                        .AllowNotGreaterThan().Name("ngt").And()
                        .AllowGreaterThanOrEquals().Name("gte").And()
                        .AllowNotGreaterThanOrEquals().Name("ngte").And()
                        .AllowLowerThan().Name("lt").And()
                        .AllowNotLowerThan().Name("nlt").And()
                        .AllowLowerThanOrEquals().Name("lte").And()
                        .AllowNotLowerThanOrEquals().Name("nlte").And();
                }));

            // assert
            schema.ToString().MatchSnapshot();
            Assert.Equal(first, second);
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
                        .Filter(x => x.BarShort)
                        .BindFiltersImplicitly();
                    descriptor
                        .BindFieldsExplicitly()
                        .Filter(x => x.BarShort)
                        .BindFiltersExplicitly().AllowNotEquals();
                }));

            // assert
            schema.ToString().MatchSnapshot();
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
                        .Filter(x => x.BarShort)
                        .BindFiltersExplicitly().AllowNotEquals();
                    descriptor.Ignore(x => x.BarShort);
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
                    descriptor.Ignore(x => x.BarShort);
                    descriptor
                        .BindFieldsExplicitly()
                        .Filter(x => x.BarShort)
                        .BindFiltersExplicitly().AllowNotEquals();
                }));

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
                descriptor
                .BindFieldsExplicitly()
                .Filter(x => x.BarShort)
                .BindFiltersExplicitly()
                .AllowEquals()
                .And().AllowNotEquals()
                .And().AllowIn()
                .And().AllowNotIn()
                .And().AllowGreaterThan()
                .And().AllowNotGreaterThan()
                .And().AllowGreaterThanOrEquals()
                .And().AllowNotGreaterThanOrEquals()
                .And().AllowLowerThan()
                .And().AllowNotLowerThan()
                .And().AllowLowerThanOrEquals()
                .And().AllowNotLowerThanOrEquals();
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
                descriptor.Filter(x => x.BarInt)
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
                descriptor.Filter(x => x.BarInt)
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
                    d.Filter(x => x.BarInt)
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
                    d.Filter(x => x.BarInt)
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
                    d.Filter(x => x.BarInt)
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
                    d.Filter(x => x.BarInt)
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
                        .Filter(x => x.BarInt)
                        .BindFiltersExplicitly()
                        .BindFiltersImplicitly();
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
                    .Ignore(f => f.BarShort)));

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
                    .Filter(f => f.BarShort)
                    .Ignore()));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Model_With_Nullable_Properties()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new FilterInputType<FooNullable>(
                    d => d.Filter(f => f.BarShort)));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Infer_Nullable_Fields()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new FilterInputType<FooNullable>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        public enum FooBar
        {
            Foo,
            Bar
        }

        public class Foo
        {
            public short BarShort { get; set; }
            public int BarInt { get; set; }
            public long BarLong { get; set; }
            public float BarFloat { get; set; }
            public double BarDouble { get; set; }
            public decimal BarDecimal { get; set; }
            public short? BarShortNullable { get; set; }
            public int? BarIntNullable { get; set; }
            public long? BarLongNullable { get; set; }
            public float? BarFloatNullable { get; set; }
            public double? BarDoubleNullable { get; set; }
            public decimal? BarDecimalNullable { get; set; }
            public FooBar FooBar { get; set; }
        }

        public class FooNullable
        {
            public short? BarShort { get; set; }
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
                descriptor.BindFieldsExplicitly();
                descriptor.Filter(x => x.BarShort);
                descriptor.Filter(x => x.BarInt);
                descriptor.Filter(x => x.BarLong);
                descriptor.Filter(x => x.BarFloat);
                descriptor.Filter(x => x.BarDouble);
                descriptor.Filter(x => x.BarDecimal);
                descriptor.Filter(x => x.BarShortNullable);
                descriptor.Filter(x => x.BarIntNullable);
                descriptor.Filter(x => x.BarLongNullable);
                descriptor.Filter(x => x.BarFloatNullable);
                descriptor.Filter(x => x.BarDoubleNullable);
                descriptor.Filter(x => x.BarDecimalNullable);
                descriptor.Filter(x => x.FooBar);
            }
        }

        public class FooFilterTypeDefaults
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
            }
        }
    }
}
