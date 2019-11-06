using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class ObjectFilterInputTypeTests
        : TypeTestBase
    {

        //
        [Fact]
        public void Create_ObjectFilter_FooImplicitBarImplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ObjectFilter_FooImplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(
                    x => x.Object(y => y.BarNested)
                    .AllowObject(
                        y => y.BindFieldsExplicitly()
                        .Filter(z => z.Baz)
                        .BindFiltersExplicitly()
                        .AllowContains()
                        )
                    )
           );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact(Skip = "Skipped till issue 1194 is solved")]
        public void Create_ObjectFilter_FooImplicitMultipleBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(
                    x =>
                    {
                        x.Object(y => y.BarNested)
                            .AllowObject(
                                y => y.BindFieldsExplicitly()
                                .Filter(z => z.Baz)
                                .BindFiltersExplicitly()
                                .AllowContains());
                        x.Object(y => y.BarNestedSecond)
                            .AllowObject(
                                y => y.BindFieldsExplicitly()
                                .Filter(z => z.Baz)
                                .BindFiltersExplicitly()
                                .AllowEquals());
                    }
                    )
           );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ObjectFilter_FooExplicitBarExplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(
                    x => x.BindFieldsExplicitly()
                    .Object(y => y.BarNested)
                    .AllowObject(
                        y => y.BindFieldsExplicitly()
                        .Filter(z => z.Baz)
                        .BindFiltersExplicitly()
                        .AllowContains()
                        )
                    )
           );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ObjectFilter_FooExplicitBarExplicitByGeneric()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(
                    x => x.BindFieldsExplicitly()
                    .Object(y => y.BarNested)
                    .AllowObject<BarFilterType>()
                    )
           );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_ObjectFilter_FooExplicitBarImplicit()
        {
            // arrange
            // act
            var schema = CreateSchema(
                new FilterInputType<Foo>(
                    x => x.BindFieldsExplicitly()
                    .Object(y => y.BarNested)
                    .AllowObject()
                    )
           );

            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void Create_ObjectFilter_BindExplicitly()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>(descriptor => descriptor.BindFieldsExplicitly()
                    .Object(x => x.BarNested)
                    .AllowObject(x =>
                        x.BindFieldsExplicitly()
                        .Filter(y => y.Baz).BindFiltersExplicitly().AllowEquals()
                    )));

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
            var schema = CreateSchema(new FilterInputType<Foo>(descriptor =>
            {
                descriptor.Object(x => x.BarNested)
                    .BindExplicitly()
                    .AllowObject();
            }));

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
                descriptor.Object(x => x.BarNested)
                    .BindExplicitly()
                    .AllowObject()
                    .Name("custom_object");
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
                descriptor.Object(x => x.BarNested)
                    .BindExplicitly()
                    .AllowObject()
                    .Description("custom_object_description");
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
                    d.Object(x => x.BarNested)
                        .BindExplicitly()
                        .AllowObject()
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
                    d.Object(x => x.BarNested)
                        .BindExplicitly()
                        .AllowObject()
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
                    d.Object(x => x.BarNested)
                        .BindExplicitly()
                        .AllowObject()
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
            public Bar BarNested { get; set; }
            public Bar BarNestedSecond { get; set; }
        }

        public class Bar
        {
            public string Baz { get; set; }
        }

        public class BarFilterType
            : FilterInputType<Bar>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Bar> descriptor)
            {
                descriptor.BindFieldsExplicitly().Filter(x => x.Baz).BindFiltersExplicitly().AllowEquals().Name("test");
            }
        }
    }
}
