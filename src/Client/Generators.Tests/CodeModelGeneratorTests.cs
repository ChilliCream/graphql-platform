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
using static System.IO.Path;

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
            var outputHandler = new TestOutputHandler();

            // act
            await ClientGenerator.New()
                .AddQueryDocumentFromString("Queries",
                    FileResource.Open(queryFile))
                .AddSchemaDocumentFromString("StarWars",
                    FileResource.Open("StarWars.graphql"))
                .SetOutput(outputHandler)
                .BuildAsync();

            // assert
            outputHandler.Content.MatchSnapshot(
                new SnapshotNameExtension(queryFile));
        }

        private class TestOutputHandler
            : IFileHandler
        {
            private readonly List<GeneratorTask> _tasks = new List<GeneratorTask>();

            public string Content { get; private set; }

            public void Register(
                ICodeDescriptor descriptor,
                ICodeGenerator generator)
            {
                _tasks.Add(new GeneratorTask
                {
                    Descriptor = descriptor,
                    Generator = new NamespaceGenerator(generator)
                });
            }

            public async Task WriteAllAsync(ITypeLookup typeLookup)
            {
                var usedNames = new HashSet<string>();

                using (var stream = new MemoryStream())
                {
                    using (var sw = new StreamWriter(stream, Encoding.UTF8))
                    {
                        using (var cw = new CodeWriter(sw))
                        {
                            foreach (GeneratorTask task in _tasks)
                            {
                                if (task.Descriptor.GetType() != typeof(QueryDescriptor))
                                {
                                    await task.Generator.WriteAsync(
                                        cw, task.Descriptor, typeLookup);
                                    await cw.WriteLineAsync();
                                    await cw.WriteLineAsync();
                                }
                            }
                        }
                    }

                    Content = Encoding.UTF8.GetString(stream.ToArray());
                }
            }

            private class GeneratorTask
            {
                public ICodeDescriptor Descriptor { get; set; }
                public ICodeGenerator Generator { get; set; }
            }
        }
    }
}
