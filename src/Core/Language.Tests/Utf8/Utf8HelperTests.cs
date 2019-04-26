using System.Text;
using System;
using Xunit;

namespace HotChocolate.Language
{
    public class Utf8HelperTests
    {
        [Fact]
        public void Unescape_NothingIsEscaped_InputIsOutput()
        {
            // arrange
            byte[] inputData = Encoding.UTF8.GetBytes("hello_123");
            byte[] outputBuffer = new byte[inputData.Length];

            var input = new ReadOnlySpan<byte>(inputData);
            var output = new Span<byte>(outputBuffer);

            // act
            Utf8Helper.Unescape(in input, ref output, false);

            // assert
            Assert.Equal(inputData, outputBuffer);
        }

        [InlineData("\\b", "\b")]
        [InlineData("\\f", "\f")]
        [InlineData("\\n", "\n")]
        [InlineData("\\r", "\r")]
        [InlineData("\\t", "\t")]
        [InlineData("\\\"", "\"")]
        [Theory]
        public void Unescape_StandardEscapeChars_OutputIsUnescaped(
            string escaped, string unescaped)
        {
            // arrange
            byte[] inputData = Encoding.UTF8.GetBytes("hello_123_" + escaped);
            byte[] outputBuffer = new byte[inputData.Length];

            var input = new ReadOnlySpan<byte>(inputData);
            var output = new Span<byte>(outputBuffer);

            // act
            Utf8Helper.Unescape(in input, ref output, false);

            // assert
            Assert.Equal("hello_123_" + unescaped,
                Encoding.UTF8.GetString(output.ToArray()));
        }

        [InlineData("\\b", "\b")]
        [InlineData("\\f", "\f")]
        [InlineData("\\n", "\n")]
        [InlineData("\\r", "\r")]
        [InlineData("\\t", "\t")]
        [InlineData("\\\"\"\"", "\"\"\"")]
        [Theory]
        public void Unescape_BlockStringEscapeChars_OutputIsUnescaped(
          string escaped, string unescaped)
        {
            // arrange
            byte[] inputData = Encoding.UTF8.GetBytes("hello_123_" + escaped);
            byte[] outputBuffer = new byte[inputData.Length];

            var input = new ReadOnlySpan<byte>(inputData);
            var output = new Span<byte>(outputBuffer);

            // act
            Utf8Helper.Unescape(in input, ref output, true);

            // assert
            Assert.Equal("hello_123_" + unescaped,
                Encoding.UTF8.GetString(output.ToArray()));
        }


        [InlineData("\\\"\"")]
        [InlineData("\\\"")]
        [Theory]
        public void Unescape_BlockStringInvalidEscapeChars_Exception(
           string escaped)
        {
            // arrange
            byte[] inputData = Encoding.UTF8.GetBytes("hello_123_" + escaped);
            byte[] outputBuffer = new byte[inputData.Length];

            var input = new ReadOnlySpan<byte>(inputData);
            var output = new Span<byte>(outputBuffer);

            // act
            try
            {
                Utf8Helper.Unescape(in input, ref output, true);

                // assert
                Assert.True(false, "The unescape method should fail.");
            }
            catch
            {
            }
        }

        [InlineData("\\u0024", "$")]
        [InlineData("\\u00A2", "¢")]
        [Theory]
        public void Unescape_UnicodeEscapeChars_OutputIsUnescaped(
           string escaped, string unescaped)
        {
            // arrange
            byte[] inputData = Encoding.UTF8.GetBytes("hello_123_" + escaped);
            byte[] outputBuffer = new byte[inputData.Length];

            var input = new ReadOnlySpan<byte>(inputData);
            var output = new Span<byte>(outputBuffer);

            // act
            Utf8Helper.Unescape(in input, ref output, false);

            // assert
            Assert.Equal("hello_123_" + unescaped,
                Encoding.UTF8.GetString(output.ToArray()));
        }
    }
}
