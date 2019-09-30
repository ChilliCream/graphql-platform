using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;
using Snapshooter.Xunit;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.CSharp;

namespace StrawberryShake.Generators
{
    public class ObjectModelGeneratorTests
        : ModelGeneratorTestBase
    {
        [Fact]
        public async Task Object_No_Fragment()
        {
            // arrange
            var path = HotChocolate.Path.New("root");

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query search {
                    droid(id: ""foo"") {
                        name
                    }
                }
                ");

            var operation = document.Definitions
                .OfType<OperationDefinitionNode>()
                .First();

            var field = operation.SelectionSet.Selections
                .OfType<FieldNode>()
                .First();

            var query = new QueryDescriptor(
                "Simple_Query",
                "Foo.Bar.Ns",
                "1234",
                "12345",
                new byte[] { 1, 2, 3 },
                document);

            var schema = SchemaBuilder.New()
                .AddDocumentFromString(FileResource.Open("StarWars.graphql"))
                .Use(next => context => Task.CompletedTask)
                .Create();

            var context = new ModelGeneratorContext(
                schema,
                query,
                "StarWarsClient",
                "Foo.Bar.Ns");

            var droid = schema.GetType<ObjectType>("Droid");

            // act
            var generator = new ObjectModelGenerator();

            generator.Generate(
                context,
                operation,
                droid,
                droid,
                field,
                context.CollectFields(droid, field.SelectionSet, path),
                path);

            // assert
            var typeLookup = new TypeLookup(
                LanguageVersion.CSharp_8_0,
                CollectFieldsVisitor.MockLookup(document, context.FieldTypes));

            string output = await WriteAllAsync(context.Descriptors, typeLookup);

            output.MatchSnapshot();
        }

        [Fact]
        public async Task Object_With_Fragment()
        {
            // arrange
            var path = HotChocolate.Path.New("root");

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query search {
                    droid(id: ""foo"") {
                        ... SomeDroid
                    }
                }

                fragment SomeDroid on Droid {
                    name
                }
                ");

            var operation = document.Definitions
                .OfType<OperationDefinitionNode>()
                .First();

            var field = operation.SelectionSet.Selections
                .OfType<FieldNode>()
                .First();

            var query = new QueryDescriptor(
                "Simple_Query",
                "Foo.Bar.Ns",
                "1234",
                "12345",
                new byte[] { 1, 2, 3 },
                document);

            var schema = SchemaBuilder.New()
                .AddDocumentFromString(FileResource.Open("StarWars.graphql"))
                .Use(next => context => Task.CompletedTask)
                .Create();

            var context = new ModelGeneratorContext(
                schema,
                query,
                "StarWarsClient",
                "Foo.Bar.Ns");

            var droid = schema.GetType<ObjectType>("Droid");

            // act
            var generator = new ObjectModelGenerator();

            generator.Generate(
                context,
                operation,
                droid,
                droid,
                field,
                context.CollectFields(droid, field.SelectionSet, path),
                path);

            // assert
            var typeLookup = new TypeLookup(
                LanguageVersion.CSharp_8_0,
                CollectFieldsVisitor.MockLookup(document, context.FieldTypes));

            string output = await WriteAllAsync(context.Descriptors, typeLookup);

            output.MatchSnapshot();
        }

        [Fact]
        public async Task Object_List_With_Fragment()
        {
            // arrange
            var path = HotChocolate.Path.New("root");

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query getBars {
                    foo {
                        bars {
                            baz
                        }
                    }
                }
                ");

            var operation = document.Definitions
                .OfType<OperationDefinitionNode>()
                .First();

            var field = operation.SelectionSet.Selections
                .OfType<FieldNode>()
                .First();

            var query = new QueryDescriptor(
                "Simple_Query",
                "Foo.Bar.Ns",
                "1234",
                "12345",
                new byte[] { 1, 2, 3 },
                document);

            var schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"
                    type Query {
                        foo: Foo
                    }

                    type Foo {
                        bars: [Bar]
                    }

                    type Bar {
                        baz: String
                    }
                    ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            var context = new ModelGeneratorContext(
                schema,
                query,
                "StarWarsClient",
                "Foo.Bar.Ns");

            var bar = schema.GetType<ObjectType>("Foo");

            // act
            var generator = new ObjectModelGenerator();

            generator.Generate(
                context,
                operation,
                bar,
                bar,
                field,
                context.CollectFields(bar, field.SelectionSet, path),
                path);

            // assert
            var typeLookup = new TypeLookup(
                LanguageVersion.CSharp_8_0,
                CollectFieldsVisitor.MockLookup(document, context.FieldTypes));

            string output = await WriteAllAsync(context.Descriptors, typeLookup);

            output.MatchSnapshot();
        }
    }
}
