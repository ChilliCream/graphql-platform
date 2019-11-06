using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    public class SortInputObjectTypeTests
        : TypeTestBase
    {
        [Fact]
        public void Create_Implicit_Sorting_NoBindInvocation()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new SortInputType<Foo>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Implicit_Sorting()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new SortInputType<Foo>(d => d.BindFieldsImplicitly()));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Implicit_Sorting_WithIgnoredField()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new SortInputType<Baz>(d => d.BindFieldsImplicitly()
                    .SortableObject(f => f.BarProperty).Ignore()));

            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void Create_Description_Explicitly()
        {
            // arrange
            // act
            var schema = CreateSchema(new SortInputType<Foo>(descriptor =>
            {
                descriptor.BindFieldsExplicitly()
                    .SortableObject(x => x.Bar)
                    .Description("custom_description");
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Directive_By_Name()
        {
            // arrange
            // act
            var schema = CreateSchema(builder =>
                builder.AddType(new SortInputType<Foo>(d =>
                {
                    d.BindFieldsExplicitly().
                    SortableObject(x => x.Bar)
                        .Directive("bar");
                }))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("bar")
                    .Location(DirectiveLocation.InputFieldDefinition))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Directive_By_Name_With_Argument()
        {
            // arrange
            // act
            var schema = CreateSchema(builder =>
                builder.AddType(new SortInputType<Foo>(d =>
                {
                    d.BindFieldsExplicitly()
                        .SortableObject(x => x.Bar)
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
        public void Create_Directive_With_Clr_Type()
        {
            // arrange
            // act
            var schema = CreateSchema(builder =>
                builder.AddType(new SortInputType<Foo>(d =>
                {
                    d.BindFieldsExplicitly()
                    .SortableObject(x => x.Bar)
                    .Directive<FooDirective>();
                }))
                .AddDirectiveType(new DirectiveType<FooDirective>(d => d
                    .Location(DirectiveLocation.InputFieldDefinition))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Implicit_Sorting_WithRenamedField()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new SortInputType<Foo>(d => d.BindFieldsImplicitly()
                    .SortableObject(f => f.Bar).Name("quux")));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Explicit_Sorting()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new SortInputType<Foo>(d => d
                    .BindFieldsExplicitly()
                    .SortableObject(f => f.Bar)
                        .Type(x => x.BindFieldsExplicitly()
                        .SortableObject(y => y.Baz)
                        .Type(z => z.BindFieldsExplicitly().Sortable(x => x.BarProperty)
                        )
                    )
                )
                );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Explicit_Sorting_ByType()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new SortInputType<Foo>(d => d
                    .BindFieldsExplicitly()
                    .SortableObject(f => f.Bar)
                        .Type<SortInputType<Bar>>()
                        )
                );

            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact(Skip= "Skipped till issue 1194 is solved")]
        public void Create_Explicit_Sorting_DifferentDescirptorOfSameType()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                new SortInputType<FooDoubleBaz>(d =>
                {
                    d.BindFieldsExplicitly()
                      .SortableObject(f => f.Baz1)
                      .Type(x => x.BindFieldsExplicitly().Sortable(y => y.BarProperty)
                            );
                    d.SortableObject(f => f.Baz2)
                    .Type(x => x.BindFieldsExplicitly()
                    .Sortable(y => y.BazProperty)
                        );
                }
                )
            );

            // assert
            schema.ToString().MatchSnapshot();
        }



        private class BarType : SortInputType<Bar>
        {
            protected override void Configure(ISortInputTypeDescriptor<Bar> descriptor)
            {
                base.Configure(descriptor);
                descriptor.BindFieldsExplicitly().Sortable(x => x.BarProperty);
            }
        }

        private class FooDirective
        {
        }
        private class Foo
        {
            public Bar Bar { get; set; }
        }

        private class Bar
        {
            public string BarProperty { get; set; }
            public Baz Baz { get; set; }
        }
        private class Baz
        {
            public string BarProperty { get; set; }
            public string BazProperty { get; set; }
        }


        private class FooDoubleBaz
        {
            public Baz Baz2 { get; set; }
            public Baz Baz1 { get; set; }
        }
    }
}
