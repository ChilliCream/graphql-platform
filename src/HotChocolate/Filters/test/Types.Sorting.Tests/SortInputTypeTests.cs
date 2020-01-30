using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    public class SortInputTypeTests
        : TypeTestBase
    {

        [Fact]
        public void Create_Global_Explicit_Sorting()
        {
            // arrange
            // act
            ISchemaBuilder builder = SchemaBuilder.New()
               .AddQueryType(c =>
                   c.Name("Query")
                       .Field("foo")
                       .Type<StringType>()
                       .Resolver("bar"))
               .AddType(new SortInputType<Foo>(
                   d => d.BindFieldsExplicitly()
                   .Sortable(f => f.Bar)
                ))
               .ModifyOptions(t => t.DefaultBindingBehavior = BindingBehavior.Explicit);

            ISchema schema = builder.Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Implicit_Sorting_EnumerablesShouldNotBeGenerated()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new SortInputType<FooEnumerables>());

            // assert
            schema.ToString().MatchSnapshot();
        }


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
                new SortInputType<Foo>(d => d.BindFieldsImplicitly()
                    .Sortable(f => f.Baz).Ignore()));

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
                    .Sortable(f => f.Baz).Name("quux")));

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
                    .Sortable(f => f.Bar)
                ));

            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void Create_Description_Explicitly()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new SortInputType<Foo>(descriptor =>
            {
                descriptor.BindFieldsExplicitly()
                    .Sortable(x => x.Bar)
                    .Description("custom_description");
            }));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInputType_DynamicName()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType(new SortInputType<Foo>(
                 d => d
                     .Name(dep => dep.Name + "Foo")
                     .DependsOn<StringType>()
                     .Sortable(x => x.Bar)
                     )
                 )
             );


            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void SortInputType_DynamicName_NonGeneric()
        {

            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType(new SortInputType<Foo>(
                 d => d
                     .Name(dep => dep.Name + "Foo")
                     .DependsOn(typeof(StringType))
                     .Sortable(x => x.Bar)
                     )
                 )
             );


            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInputType_AddDirectives_NameArgs()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddDirectiveType<FooDirectiveType>()
             .AddType(new SortInputType<Foo>(
                 d => d.Directive("foo")
                     .Sortable(x => x.Bar)
                     )
                )
            );


            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInputType_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddDirectiveType<FooDirectiveType>()
             .AddType(new SortInputType<Foo>(
               d => d.Directive(new NameString("foo"))
                    .Sortable(x => x.Bar)
                    )
                )
            );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Directive_By_Name()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(builder =>
                builder.AddType(new SortInputType<Foo>(d =>
                {
                    d.BindFieldsExplicitly().
                    Sortable(x => x.Bar)
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
            ISchema schema = CreateSchema(builder =>
                builder.AddType(new SortInputType<Foo>(d =>
                {
                    d.BindFieldsExplicitly()
                        .Sortable(x => x.Bar)
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
            ISchema schema = CreateSchema(builder =>
                builder.AddType(new SortInputType<Foo>(d =>
                {
                    d.BindFieldsExplicitly()
                    .Sortable(x => x.Bar)
                    .Directive<FooDirective>();
                }))
                .AddDirectiveType(new DirectiveType<FooDirective>(d => d
                    .Location(DirectiveLocation.InputFieldDefinition))));

            // assert
            schema.ToString().MatchSnapshot();
        }


        [Fact]
        public void SortInputType_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddDirectiveType<FooDirectiveType>()
             .AddType(new SortInputType<Foo>(
                d => d
                    .Directive(new DirectiveNode("foo"))
                     .Sortable(x => x.Bar)
                     )
                )
            );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInputType_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddDirectiveType<FooDirectiveType>()
             .AddType(new SortInputType<Foo>(
                 d => d
                     .Directive(new FooDirective())
                     .Sortable(x => x.Bar)
                     )
                )
            );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInputType_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddDirectiveType<FooDirectiveType>()
             .AddType(new SortInputType<Foo>(
                d => d
                .Directive<FooDirective>()
                 .Sortable(x => x.Bar)
                 )
                )
             );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInputType_AddDescription()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType(new SortInputType<Foo>(
                d => d.Description("Test")
                 .Sortable(x => x.Bar)
                 )
                )
             );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInputType_AddName()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType(new SortInputType<Foo>(
                d => d.Name("Test")
                 .Sortable(x => x.Bar)
                 )
                )
             );

            // assert
            schema.ToString().MatchSnapshot();
        }

        private class FooDirectiveType
          : DirectiveType<FooDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<FooDirective> descriptor)
            {
                descriptor.Name("foo");
                descriptor.Location(DirectiveLocation.InputObject)
                    .Location(DirectiveLocation.InputFieldDefinition);
            }
        }
        private class FooDirective { }

        private class Foo
        {
            public bool? NullableBoolean { get; set; }
            public string Bar { get; set; }
            public string Baz { get; set; }
        }

        private class FooEnumerables
        {
            public string Bar { get; set; }
            public List<string> List { get; set; }
            public IEnumerable<string> IEnumerable { get; set; }
            public HashSet<string> HashSet { get; set; }
            public Queue<string> Queue { get; set; }
            public Stack<string> Stack { get; set; }
        }
    }
}
