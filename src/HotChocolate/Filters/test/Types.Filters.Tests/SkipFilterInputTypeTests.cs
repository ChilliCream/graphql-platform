using System.Collections.Generic;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class SkipFilterInputTypeTests
        : TypeTestBase
    {
        [Fact]
        public void Create_Skip_Discover_Operators_Implicitly()
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
        public void Create_Skip_Declare_Type()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new FilterInputType<Foo>(
                descriptor => descriptor.Skip(x => x.Bar).Type<StringType>()));

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
                descriptor.Skip(x => x.Bar)
                    .Type<StringType>()
                    .Name("Skip_equals");
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void CreateNamed_Explicit_Filters()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new FilterInputType(d => d
                    .Name("FilterTypeName")
                    .Skip("bar")
                        .Type<StringType>()
                        .Name("foo_eq")));

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
                descriptor.Skip(x => x.Bar).Type<StringType>()
                    .Description("Skip_equals_description");
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
                    d.Skip(x => x.Bar).Type<StringType>()
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
                    d.Skip(x => x.Bar).Type<StringType>()
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
                    d.Skip(x => x.Bar).Type<StringType>()
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
                builder.AddType(new FilterInputType<Foo>(
                    d => d.Skip(x => x.Bar).Type<StringType>().Directive(new Bar())))
                .AddDirectiveType(new DirectiveType<Bar>(
                    d => d.Location(DirectiveLocation.InputFieldDefinition))));

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
                        .Skip(x => x.Bar).Type<StringType>();
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
        public void Ignore_Field_2()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new FilterInputType<Foo>(d => d
                    .Skip(f => f.Bar)
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
                descriptor.Skip(x => x.Bar).Type<StringType>();
            }
        }
    }
}
