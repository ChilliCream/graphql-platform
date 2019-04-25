using System.Collections.Generic;
using System.Text;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class Utf8ReaderTests
    {
        [Fact]
        public void Read_NameBraceTokens()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes("{ x { y } }");

            // act
            var tokens = new List<SyntaxTokenInfo>();
            var reader = new Utf8GraphQLReader(sourceText);

            do
            {
                tokens.Add(SyntaxTokenInfo.FromReader(in reader));
            }
            while (reader.Read());

            // assert
            tokens.MatchSnapshot();
        }

        [Fact]
        public void Read_Comment()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                "{ #test me foo bar \n me }");

            // act
            var tokens = new List<SyntaxTokenInfo>();
            var reader = new Utf8GraphQLReader(sourceText);

            do
            {
                tokens.Add(SyntaxTokenInfo.FromReader(in reader));
            }
            while (reader.Read());

            // assert
            tokens.MatchSnapshot();
        }

        [Fact]
        public void Read_StringValue()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                "{ me(a: \"Abc¢def\\n\") }");

            // act
            var tokens = new List<SyntaxTokenInfo>();
            var reader = new Utf8GraphQLReader(sourceText);

            do
            {
                tokens.Add(SyntaxTokenInfo.FromReader(in reader));
            }
            while (reader.Read());

            // assert
            tokens.MatchSnapshot();
        }

        [Fact]
        public void Read_BlockStringValue()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                "{ me(a: \"\"\"\n     Abcdef\n\"\"\") }");

            // act
            var tokens = new List<SyntaxTokenInfo>();
            var reader = new Utf8GraphQLReader(sourceText);

            do
            {
                tokens.Add(SyntaxTokenInfo.FromReader(in reader));
            }
            while (reader.Read());

            // assert
            tokens.MatchSnapshot();
        }
    }
}
