using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Language;
using Moq;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.Generators
{
    public class DocumentVisitorTests
    {
        [InlineData("Simple_Query.graphql")]
        // [InlineData("Spread_Query.graphql")]
        // [InlineData("Multiple_Fragments_Query.graphql")]
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

            var generator = new FooGen(schema, query);

            // act
            generator.GenerateModels();



            // assert

        }
    }
}
