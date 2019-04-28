﻿using System.Text;
using Xunit;

namespace HotChocolate.Language
{
    public class Utf8NumberTokenReaderTests
    {
        [InlineData("1234.123", true)]
        [InlineData("-1234.123", true)]
        [InlineData("1234", false)]
        [InlineData("-1234", false)]
        [InlineData("1e50", true)]
        [InlineData("6.0221413e23", true)]
        [Theory]
        private void ReadToken(string sourceBody, bool isFloat)
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(sourceBody);
            var reader = new Utf8GraphQLReader(source);

            // act
            reader.Read();

            // assert
            Assert.Equal(sourceBody, reader.GetScalarValue());
            Assert.Equal(
                isFloat ? TokenKind.Float : TokenKind.Integer,
                reader.Kind);
            Assert.Equal(1, reader.Line);
            Assert.Equal(1, reader.Column);
            Assert.Equal(0, reader.Start);
            Assert.Equal(sourceBody.Length, reader.End);
        }
    }
}
