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

        [Fact]
        public void Foo()
        {
            const int initial_a = 0b1010_1111_1111_1111;
            const int initial_b = 0b0001_1111_1111_1111;
            const int initial_c = 0b0010_1111_1111_1111;

            const int mask = 0b1111_0000_0000_0000;

            int a = initial_b & mask;
            a = a >> 12;
            a = a << 12;

            int b = initial_b - a;







            /*
                    const int mask = 0b1_1111_0000_0000_0000;
                    const int mask2 = 0b1_0000_0000_0000_0000;
                    const int mask3 = 0b0000_1111_1111_1111;


                    int a = (initial ^ mask);
                    a = a >> 12;
                    a = a << 12;
                    a = (a ^ mask2);

                    string binary = Convert.ToString(a, 2);
                    a = a | mask3;




                    binary = Convert.ToString(a, 2);

         */
        }
    }
}
