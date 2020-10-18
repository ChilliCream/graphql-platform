using System.Collections.Generic;
using System.Linq;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Tests
{
    public class SortInputTypeTest
        : SortTestBase
    {
        [Fact]
        public void SortInputType_DynamicName()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                s => s.AddType(
                    new SortInputType<Foo>(
                        d => d
                            .Name(dep => dep.Name + "Foo")
                            .DependsOn<StringType>()
                            .Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInputType_DynamicName_NonGeneric()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                s => s.AddType(
                    new SortInputType<Foo>(
                        d => d.Name(dep => dep.Name + "Foo")
                            .DependsOn(typeof(StringType))
                            .Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInput_AddDirectives_NameArgs()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                s => s.AddDirectiveType<FooDirectiveType>()
                    .AddType(
                        new SortInputType<Foo>(
                            d => d.Directive("foo")
                                .Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInput_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                s => s.AddDirectiveType<FooDirectiveType>()
                    .AddType(
                        new SortInputType<Foo>(
                            d => d.Directive(new NameString("foo"))
                                .Field(x => x.Bar)
                        )
                    )
            );

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInput_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                s => s.AddDirectiveType<FooDirectiveType>()
                    .AddType(
                        new SortInputType<Foo>(
                            d => d.Directive(new DirectiveNode("foo")).Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInput_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                s => s.AddDirectiveType<FooDirectiveType>()
                    .AddType(
                        new SortInputType<Foo>(
                            d => d
                                .Directive(new FooDirective())
                                .Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInput_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                s => s.AddDirectiveType<FooDirectiveType>()
                    .AddType(
                        new SortInputType<Foo>(
                            d => d
                                .Directive<FooDirective>()
                                .Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInput_AddDescription()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                s => s.AddType(
                    new SortInputType<Foo>(
                        d => d.Description("Test").Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInput_AddName()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(
                s => s.AddType(
                    new SortInputType<Foo>(
                        d => d.Name("Test").Field(x => x.Bar))));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void SortInputType_Should_ThrowException_WhenNoConventionIsRegistered()
        {
            // arrange
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Resolve(new List<Foo>())
                        .UseSorting("Foo"));

            // act
            // assert
            SchemaException exception = Assert.Throws<SchemaException>(() => builder.Create());
            exception.Message.MatchSnapshot();
        }

        [Fact]
        public void SortInputType_Should_ThrowException_WhenNoConventionIsRegisteredDefault()
        {
            // arrange
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Resolve(new List<Foo>())
                        .UseSorting());

            // act
            // assert
            SchemaException exception = Assert.Throws<SchemaException>(() => builder.Create());
            exception.Message.MatchSnapshot();
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

        public class FooDirective
        {
        }

        public class Foo
        {
            public string Bar { get; set; } = default!;
        }

        public class Query
        {
            [GraphQLNonNullType]
            public IQueryable<Book> Books() => new List<Book>().AsQueryable();
        }

        public class Book
        {
            public int Id { get; set; } = default!;

            [GraphQLNonNullType]
            public string Title { get; set; } = default!;

            public int Pages { get; set; } = default!;

            public int Chapters { get; set; } = default!;

            [GraphQLNonNullType]
            public Author Author { get; set; } = default!;
        }

        public class Author
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public int Id { get; set; }

            [GraphQLNonNullType]
            public string Name { get; set; } = default!;
        }
    }
}
