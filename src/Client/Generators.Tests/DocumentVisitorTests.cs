using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Language;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.Generators
{
    public class DocumentVisitorTests
    {
        [Fact]
        public async Task Foo()
        {
            // arrange
            var schema = SchemaBuilder.New()
                .AddDocumentFromString(FileResource.Open("StarWars.graphql"))
                .Use(next => context => Task.CompletedTask)
                .Create();

            DocumentNode query = Utf8GraphQLParser.Parse(
                FileResource.Open("Query.graphql"));

            var list = new List<Func<Stream, Task>>();

            var fileHandler = new Mock<IFileHandler>();
            fileHandler.Setup(t => t.WriteTo(
                It.IsAny<string>(), It.IsAny<Func<Stream, Task>>()))
                .Callback(new Action<string, Func<Stream, Task>>((s, d) => list.Add(d)));

            // act
            var visitor = new DocumentVisitor(schema, fileHandler.Object);
            query.Accept(visitor, n => VisitorAction.Continue);

            // assert
            var stream = new MemoryStream();
            foreach (var f in list)
            {
                await f(stream);
            }

            Encoding.UTF8.GetString(stream.ToArray()).MatchSnapshot();
        }
    }
}
