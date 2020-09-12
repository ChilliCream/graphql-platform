using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterInputTypeTest
        : TypeTestBase
    {

        [Fact]
        public void FilterInputType_DynamicName()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType(
                new FilterInputType<Foo>(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn<StringType>()
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInputType_DynamicName_NonGeneric()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType(
                new FilterInputType<Foo>(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType))
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));


            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddDirectives_NameArgs()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s
                .AddDirectiveType<FooDirectiveType>()
                .AddType(new FilterInputType<Foo>(d => d
                    .Directive("foo")
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s
                .AddDirectiveType<FooDirectiveType>()
                .AddType(new FilterInputType<Foo>(d => d
                    .Directive(new NameString("foo"))
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s
                .AddDirectiveType<FooDirectiveType>()
                .AddType(new FilterInputType<Foo>(d => d
                    .Directive(new DirectiveNode("foo"))
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s
                .AddDirectiveType<FooDirectiveType>()
                .AddType(new FilterInputType<Foo>(d => d
                    .Directive(new FooDirective())
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s
                .AddDirectiveType<FooDirectiveType>()
                .AddType(new FilterInputType<Foo>(d => d
                    .Directive<FooDirective>()
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddDescription()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s
                .AddType(new FilterInputType<Foo>(d => d
                    .Description("Test")
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddName()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s
                .AddType(new FilterInputType<Foo>(d => d
                    .Name("Test")
                    .Filter(x => x.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals())));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterAttribute_NonNullType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(d => d
                    .Name("Test")
                    .Field(x => x.Books())
                    .UseFiltering())
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class FooDirectiveType
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

        public class FooDirective { }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class Query
        {
            [GraphQLNonNullType(false, false)]
            public IQueryable<Book> Books() => new List<Book>().AsQueryable();
        }

        public class Book
        {
            public int Id { get; set; }
            
            [GraphQLNonNullType]
            public string Title { get; set; }
            
            public int Pages { get; set; }
            
            public int Chapters { get; set; }
            
            [GraphQLNonNullType]
            public Author Author { get; set; }
        }

        public class Author
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public int Id { get; set; }
            
            [GraphQLNonNullType]
            public string Name { get; set; }
        }
    }
}
