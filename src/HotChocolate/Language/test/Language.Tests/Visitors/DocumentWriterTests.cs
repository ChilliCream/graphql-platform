using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Language
{
    public class DocumentWriterTests
    {
        [Fact]
        public void Create_StringBuilderNull_ShouldThrowArgumentException()
        {
            // arrange
            // act
            Action a = () => new DocumentWriter((StringBuilder)null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void Create_StringWriterNull_ShouldThrowArgumentException()
        {
            // arrange
            // act
            Action a = () => new DocumentWriter((StringWriter)null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }


        [Fact]
        public void WriteSpace()
        {
            // arrange
            var stringBuilder = new StringBuilder();
            var writer = new DocumentWriter(stringBuilder);

            // act
            writer.WriteSpace();

            // assert
            Assert.Equal(" ", stringBuilder.ToString());
        }

        [Fact]
        public async Task WriteSpaceAsync()
        {
            // arrange
            var stringBuilder = new StringBuilder();
            var writer = new DocumentWriter(stringBuilder);

            // act
            await writer.WriteSpaceAsync();

            // assert
            Assert.Equal(" ", stringBuilder.ToString());
        }

        [Fact]
        public void WriteIndentation()
        {
            // arrange
            var stringBuilder = new StringBuilder();
            var writer = new DocumentWriter(stringBuilder);

            // act
            writer.WriteIndentation();

            // assert
            Assert.Equal(string.Empty, stringBuilder.ToString());
        }

        [Fact]
        public void Indent_WriteIndentation()
        {
            // arrange
            var stringBuilder = new StringBuilder();
            var writer = new DocumentWriter(stringBuilder);

            // act
            writer.Indent();
            writer.WriteIndentation();

            // assert
            Assert.Equal("  ", stringBuilder.ToString());
        }


        [Fact]
        public void Indent_WriteIndentation_Unindent_WriteIndentation()
        {
            // arrange
            var stringBuilder = new StringBuilder();
            var writer = new DocumentWriter(stringBuilder);

            // act
            writer.Indent();
            writer.WriteIndentation();
            writer.WriteLine();
            writer.Unindent();
            writer.WriteIndentation();

            // assert
            Assert.Equal(
                "  " + writer.NewLine + string.Empty,
                stringBuilder.ToString());
        }

        [Fact]
        public async Task WriteIndentationAsync()
        {
            // arrange
            var stringBuilder = new StringBuilder();
            var writer = new DocumentWriter(stringBuilder);

            // act
            await writer.WriteIndentationAsync();

            // assert
            Assert.Equal(string.Empty, stringBuilder.ToString());
        }

        [Fact]
        public async Task Indent_WriteIndentationAsync()
        {
            // arrange
            var stringBuilder = new StringBuilder();
            var writer = new DocumentWriter(stringBuilder);

            // act
            writer.Indent();
            await writer.WriteIndentationAsync();

            // assert
            Assert.Equal("  ", stringBuilder.ToString());
        }

        [Fact]
        public async Task Indent_WriteIndentation_Unindent_WriteIndentAsync()
        {
            // arrange
            var stringBuilder = new StringBuilder();
            var writer = new DocumentWriter(stringBuilder);

            // act
            writer.Indent();
            await writer.WriteIndentationAsync();
            await writer.WriteLineAsync();
            writer.Unindent();
            await writer.WriteIndentationAsync();

            // assert
            Assert.Equal(
                "  " + writer.NewLine + string.Empty,
                stringBuilder.ToString());
        }

    }
}
