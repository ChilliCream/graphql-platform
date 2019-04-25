using System;
using System.Collections.Generic;
using System.Text;
using ChilliCream.Testing;
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

        [Fact]
        public void Read_KitchenSinkQuery()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                FileResource.Open("kitchen-sink.graphql"));

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

    public class BlockStringHelperTests
    {
        [Fact]
        public void Foo()
        {
            string blockString = FileResource.Open("BlockString.txt");
            byte[] input = Encoding.UTF8.GetBytes(blockString);
            var output = new Span<byte>(new byte[input.Length]);

            BlockStringHelper.TrimBlockStringToken(input, ref output);
        }
    }
}
