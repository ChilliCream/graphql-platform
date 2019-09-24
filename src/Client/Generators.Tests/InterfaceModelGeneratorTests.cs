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
    public class InterfaceModelGeneratorTests
        : ModelGeneratorTestBase
    {
        [Fact]
        public async Task Interface_No_Fragments()
        {
            // arrange
            var path = HotChocolate.Path.New("root");

            DocumentNode document = Utf8GraphQLParser.Parse(
                FileResource.Open("Simple_Query.graphql"));

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

            var character = schema.GetType<InterfaceType>("Character");

            // act
            var generator = new InterfaceModelGenerator();

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
        public async Task Interface_With_Fragments()
        {
            // arrange
            var path = HotChocolate.Path.New("root");

            DocumentNode document = Utf8GraphQLParser.Parse(
                FileResource.Open("Multiple_Fragments_Query.graphql"));

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

            var character = schema.GetType<InterfaceType>("Character");

            // act
            var generator = new InterfaceModelGenerator();

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
        public async Task Interface_Two_Cases()
        {
            // arrange
            var path = HotChocolate.Path.New("root");

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query getHero {
                    hero {
                        ...Hero
                    }
                }

                fragment Hero on Character {
                    ...HasName
                    ...SomeDroid
                    ...SomeHuman
                }

                fragment SomeDroid on Droid {
                    primaryFunction
                }

                fragment SomeHuman on Human {
                    homePlanet
                }

                fragment HasName on Character {
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

            var character = schema.GetType<InterfaceType>("Character");

            // act
            var generator = new InterfaceModelGenerator();

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
        public async Task Interface_Two_Cases_2()
        {
            // arrange
            var path = HotChocolate.Path.New("root");

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query getHero {
                    hero {
                        ...HasName
                        ...SomeDroid
                        ...SomeHuman
                    }
                }

                fragment SomeDroid on Droid {
                    primaryFunction
                }

                fragment SomeHuman on Human {
                    homePlanet
                }

                fragment HasName on Character {
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

            var character = schema.GetType<InterfaceType>("Character");

            // act
            var generator = new InterfaceModelGenerator();

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
        public async Task Interface_Two_Cases_3()
        {
            // arrange
            var path = HotChocolate.Path.New("root");

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query getHero {
                    hero {
                        ...Hero
                    }
                }

                fragment Hero on Character {
                    ...HasName
                    ...SomeDroid
                    ...SomeHuman
                }

                fragment SomeDroid on Droid {
                    ...HasName
                    primaryFunction
                }

                fragment SomeHuman on Human {
                    ...HasName
                    homePlanet
                }

                fragment HasName on Character {
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

            var character = schema.GetType<InterfaceType>("Character");

            // act
            var generator = new InterfaceModelGenerator();

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
