using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Language;
using Moq;
using Snapshooter;
using Snapshooter.Xunit;
using StrawberryShake.Generators.CSharp;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using Xunit;

namespace StrawberryShake.Generators
{
    public class CodeModelGeneratorTests
    {
        // [InlineData("Simple_Query.graphql")]
        //[InlineData("Spread_Query.graphql")]
        [InlineData("Multiple_Fragments_Query.graphql")]
        [Theory]
        public async Task Generate_Models(string queryFile)
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(FileResource.Open("StarWars.graphql"))
                .Use(next => context => Task.CompletedTask)
                .Create();

            DocumentNode query = Utf8GraphQLParser.Parse(
                FileResource.Open(queryFile));

            var fileActions = new List<Func<Stream, Task>>();

            var fileHandler = new Mock<IFileHandler>();
            fileHandler.Setup(t => t.WriteTo(
                It.IsAny<string>(), It.IsAny<Func<Stream, Task>>()))
                .Callback(new Action<string, Func<Stream, Task>>(
                    (s, d) => fileActions.Add(d)));

            // act
            var generator = new CodeModelGenerator(schema, query);
            generator.Generate();

            // assert
            var typeLookup = new TypeLookup(generator.FieldTypes);

            var builder = new StringBuilder();
            using (var writer = new CodeWriter(builder))
            {
                var interfaceGenerator = new InterfaceGenerator();
                var classGenerator = new ClassGenerator();
                var resultParserGenerator = new ResultParserGenerator(
                    new Dictionary<string, string>
                    {
                        { "String", "StringValueSerializer" }
                    });

                foreach (ICodeDescriptor descriptor in generator.Descriptors)
                {
                    switch (descriptor)
                    {
                        case InterfaceDescriptor i:
                            // await interfaceGenerator.WriteAsync(
                            //    writer, i, typeLookup);
                            break;
                        case ClassDescriptor c:
                            // await classGenerator.WriteAsync(
                            //    writer, c, typeLookup);
                            break;
                        case ResultParserDescriptor m:
                            await resultParserGenerator.WriteAsync(
                                writer, m, typeLookup);
                            break;
                    }

                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync();
                }
            }

            builder.ToString().MatchSnapshot(
                new SnapshotNameExtension(queryFile + ".cs"));
        }
    }
}
