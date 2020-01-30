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
        public void Read_Two_NameTokens()
        {
            var source = new ReadOnlySpan<byte>(
                Encoding.UTF8.GetBytes("type foo"));
            var lexer = new Utf8GraphQLReader(source);

            Assert.Equal(TokenKind.StartOfFile, lexer.Kind);

            Assert.True(lexer.Read());
            Assert.Equal(TokenKind.Name, lexer.Kind);
            Assert.Equal("type",
                Encoding.UTF8.GetString(lexer.Value.ToArray()));

            Assert.True(lexer.Read());
            Assert.Equal(TokenKind.Name, lexer.Kind);
            Assert.Equal("foo",
                Encoding.UTF8.GetString(lexer.Value.ToArray()));

            Assert.False(lexer.Read());
            Assert.Equal(TokenKind.EndOfFile, lexer.Kind);
        }

        [Fact]
        public void Read_NameBraceTokens()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes("{ x { y } }");

            // act
            var tokens = new List<SyntaxTokenInfo>();
            var reader = new Utf8GraphQLReader(sourceText);

            while (reader.Read())
            {
                tokens.Add(SyntaxTokenInfo.FromReader(in reader));
            }

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

            while (reader.Read())
            {
                tokens.Add(SyntaxTokenInfo.FromReader(in reader));
            }

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

            while (reader.Read())
            {
                tokens.Add(SyntaxTokenInfo.FromReader(in reader));
            }

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

            while (reader.Read())
            {
                tokens.Add(SyntaxTokenInfo.FromReader(in reader));
            }

            // assert
            tokens.MatchSnapshot();
        }

        [Fact]
        public void Read_KitchenSinkQuery()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                FileResource.Open("kitchen-sink.graphql")
                    .NormalizeLineBreaks());

            // act
            var tokens = new List<SyntaxTokenInfo>();
            var reader = new Utf8GraphQLReader(sourceText);

            while (reader.Read())
            {
                tokens.Add(SyntaxTokenInfo.FromReader(in reader));
            }

            // assert
            tokens.MatchSnapshot();
        }

        [Fact]
        public void Read_BlockString_SkipEscapes()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                "abc \"\"\"def\\\"\"\"\"\"\" ghi");

            // act
            var tokens = new List<SyntaxTokenInfo>();
            var reader = new Utf8GraphQLReader(sourceText);

            while (reader.Read())
            {
                tokens.Add(SyntaxTokenInfo.FromReader(in reader));
            }

            // assert
            tokens.MatchSnapshot();
        }

        [Fact]
        public void Read_String_SkipEscapes()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                "abc \"def\\\"\" ghi");

            // act
            var tokens = new List<SyntaxTokenInfo>();
            var reader = new Utf8GraphQLReader(sourceText);

            while (reader.Read())
            {
                tokens.Add(SyntaxTokenInfo.FromReader(in reader));
            }

            // assert
            tokens.MatchSnapshot();
        }

        [Fact]
        public void Skip_Boml()
        {
            // arrange
            byte[] sourceText = new[]
            {
                (byte)239,
                (byte)187,
                (byte)191,
                (byte)'a',
                (byte)'b',
                (byte)'c'
            };


            var tokens = new List<SyntaxTokenInfo>();
            var reader = new Utf8GraphQLReader(sourceText);

            while (reader.Read())
            {
                tokens.Add(SyntaxTokenInfo.FromReader(in reader));
            }

            // assert
            Assert.Collection(tokens,
                t => Assert.Equal(TokenKind.Name, t.Kind));
        }
    }
}
