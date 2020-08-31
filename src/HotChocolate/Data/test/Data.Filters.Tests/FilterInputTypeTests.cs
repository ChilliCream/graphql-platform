using System.Collections.Generic;
using System.Linq;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Tests
{
    public class FilterInputTypeTest
            : FilterTestBase
    {
        [Fact]
        public void FilterInputType_DynamicName()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType(new FilterInputType<Foo>(
                 d => d
                     .Name(dep => dep.Name + "Foo")
                     .DependsOn<StringType>()
                     .Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInputType_DynamicName_NonGeneric()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType(new FilterInputType<Foo>(
                d => d.Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType))
                    .Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddDirectives_NameArgs()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddDirectiveType<FooDirectiveType>()
             .AddType(new FilterInputType<Foo>(
                 d => d.Directive("foo")
                     .Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddDirectiveType<FooDirectiveType>()
             .AddType(new FilterInputType<Foo>(
               d => d.Directive(new NameString("foo"))
                    .Field(x => x.Bar)
                    )
                )
            );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddDirectiveType<FooDirectiveType>()
                .AddType(
                    new FilterInputType<Foo>(
                        d => d.Directive(new DirectiveNode("foo")).Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddDirectiveType<FooDirectiveType>()
                .AddType(new FilterInputType<Foo>(d => d
                    .Directive(new FooDirective())
                    .Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddDirectiveType<FooDirectiveType>()
                .AddType(new FilterInputType<Foo>(d => d
                    .Directive<FooDirective>()
                    .Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddDescription()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType(new FilterInputType<Foo>(
                d => d.Description("Test").Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterInput_AddName()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(s => s.AddType(new FilterInputType<Foo>(
                d => d.Name("Test").Field(x => x.Bar))));

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
                descriptor.Location(Types.DirectiveLocation.InputObject)
                    .Location(Types.DirectiveLocation.InputFieldDefinition);
            }
        }

        public class FooDirective { }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class Query
        {
            [GraphQLNonNullType]
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
