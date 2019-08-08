using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Snapshooter.Xunit;
using HotChocolate.Types;
using IOPath = System.IO.Path;

namespace HotChocolate
{
    public class SchemaBuilderExtensionsDocumentTests
    {
        [Fact]
        public void AddDocumentFromFile_Builder_Is_Null()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilderExtensions.AddDocumentFromFile(null, "abc");

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddDocumentFromFile_File_Is_Null()
        {
            // arrange
            var builder = SchemaBuilder.New();

            // act
            Action action = () =>
                SchemaBuilderExtensions.AddDocumentFromFile(builder, null);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddDocumentFromFile_File_Is_Empty()
        {
            // arrange
            var builder = SchemaBuilder.New();

            // act
            Action action = () =>
                SchemaBuilderExtensions.AddDocumentFromFile(
                    builder, string.Empty);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public async Task AddDocumentFromFile()
        {
            // arrange
            var builder = SchemaBuilder.New();
            string file = IOPath.GetTempFileName();
            await File.WriteAllTextAsync(file, "type Query { a: String }");

            // act
            SchemaBuilderExtensions.AddDocumentFromFile(builder, file);

            // assert
            ISchema schema = builder
                .Use(next => context => next.Invoke(context))
                .Create();

            schema.ToString().MatchSnapshot();
        }
    }
}

