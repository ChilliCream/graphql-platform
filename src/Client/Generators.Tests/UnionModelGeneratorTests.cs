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
    public class UnionModelGeneratorTests
        : ModelGeneratorTestBase
    {
        [Fact]
        public async Task Union_Inline_Fragments()
        {
            // arrange
            var path = HotChocolate.Path.New("root");

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query search {
                    search(text: ""foo"") {
                        ... on Human {
                            homePlanet
                        }
                        ... on Droid {
                            primaryFunction
                        }
                        ... on Starship {
                            name
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
                .AddDocumentFromString(FileResource.Open("StarWars.graphql"))
                .Use(next => context => Task.CompletedTask)
                .Create();

            var context = new ModelGeneratorContext(
                schema,
                query,
                "StarWarsClient",
                "Foo.Bar.Ns");

            var character = schema.GetType<UnionType>("SearchResult");

            // act
            var generator = new UnionModelGenerator();

            generator.Generate(
                context,
                operation,
                character,
                character,
                field,
                context.CollectFields(character, field.SelectionSet, path),
                path);

            // assert
            var typeLookup = new TypeLookup(
                LanguageVersion.CSharp_8_0,
                CollectFieldsVisitor.MockLookup(document, context.FieldTypes));

            string output = await WriteAllAsync(context.Descriptors, typeLookup);

            output.MatchSnapshot();
        }

        [Fact]
        public async Task Union_Fragment_Definition()
        {
            // arrange
            var path = HotChocolate.Path.New("root");

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query search {
                    search(text: ""foo"") {
                        ... SomeHuman
                        ... SomeDroid
                        ... SomeStarship
                    }
                }

                fragment SomeHuman on Human {
                    homePlanet
                }

                fragment SomeDroid on Droid {
                    primaryFunction
                }

                fragment SomeStarship on Starship {
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

            var character = schema.GetType<UnionType>("SearchResult");

            // act
            var generator = new UnionModelGenerator();

            generator.Generate(
                context,
                operation,
                character,
                character,
                field,
                context.CollectFields(character, field.SelectionSet, path),
                path);

            // assert
            var typeLookup = new TypeLookup(
                LanguageVersion.CSharp_8_0,
                CollectFieldsVisitor.MockLookup(document, context.FieldTypes));

            string output = await WriteAllAsync(context.Descriptors, typeLookup);

            output.MatchSnapshot();
        }

        [Fact]
        public async Task Union_Inline_Fragments_Skip_One_Type()
        {
            // arrange
            var path = HotChocolate.Path.New("root");

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query search {
                    search(text: ""foo"") {
                        ... on Human {
                            homePlanet
                        }
                        ... on Droid {
                            primaryFunction
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
                .AddDocumentFromString(FileResource.Open("StarWars.graphql"))
                .Use(next => context => Task.CompletedTask)
                .Create();

            var context = new ModelGeneratorContext(
                schema,
                query,
                "StarWarsClient",
                "Foo.Bar.Ns");

            var character = schema.GetType<UnionType>("SearchResult");

            // act
            var generator = new UnionModelGenerator();

            generator.Generate(
                context,
                operation,
                character,
                character,
                field,
                context.CollectFields(character, field.SelectionSet, path),
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
