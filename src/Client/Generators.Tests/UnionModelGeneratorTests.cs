using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Language;
using StrawberryShake.Generators.Descriptors;
using Xunit;
using HotChocolate.Types;
using StrawberryShake.Generators.CSharp;
using System.IO;
using StrawberryShake.Generators.Utilities;
using System.Text;
using Snapshooter.Xunit;

namespace StrawberryShake.Generators
{
    public class UnionModelGeneratorTests
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

        private static async Task<string> WriteAllAsync(
            IReadOnlyCollection<ICodeDescriptor> descriptors,
            ITypeLookup typeLookup)
        {
            var generators = new ICodeGenerator[]
            {
                new InterfaceGenerator(),
                new ClassGenerator()
            };

            using (var stream = new MemoryStream())
            {
                using (var sw = new StreamWriter(stream, Encoding.UTF8))
                {
                    using (var cw = new CodeWriter(sw))
                    {
                        foreach (ICodeGenerator generator in generators)
                        {
                            foreach (ICodeDescriptor descriptor in descriptors)
                            {
                                if (generator.CanHandle(descriptor))
                                {
                                    await generator.WriteAsync(
                                        cw, descriptor, typeLookup);
                                    await cw.WriteLineAsync();
                                }
                            }
                        }
                    }
                }
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private class CollectFieldsVisitor
            : QuerySyntaxWalker<Dictionary<FieldNode, string>>
        {
            protected override void VisitField(
                FieldNode node,
                Dictionary<FieldNode, string> context)
            {
                if (!context.ContainsKey(node))
                {
                    context.Add(node, "UNKNOWN");
                }

                base.VisitField(node, context);
            }

            public static IReadOnlyDictionary<FieldNode, string> MockLookup(
                DocumentNode query,
                IReadOnlyDictionary<FieldNode, string> knownFieldTypes)
            {
                var fieldTypes = knownFieldTypes.ToDictionary(t => t.Key, t => t.Value);
                var visitor = new CollectFieldsVisitor();
                visitor.Visit(query, fieldTypes);
                return fieldTypes;
            }
        }
    }
}
