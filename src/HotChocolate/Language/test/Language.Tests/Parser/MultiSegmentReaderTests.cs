using System.Buffers;
using System.Text;

namespace HotChocolate.Language;

public class MultiSegmentReaderTests
{
    [Fact]
    public void SingleSegment_Sequence_Matches_Span()
    {
        // arrange
        var sourceText = "{ x { y } }"u8.ToArray();
        var sequence = new ReadOnlySequence<byte>(sourceText);

        // act - read with span
        var spanTokens = new List<SyntaxTokenInfo>();
        var spanReader = new Utf8GraphQLReader(sourceText.AsSpan());
        while (spanReader.Read())
        {
            spanTokens.Add(SyntaxTokenInfo.FromReader(spanReader));
        }

        // act - read with single-segment sequence
        var seqTokens = new List<SyntaxTokenInfo>();
        var seqReader = new Utf8GraphQLReader(sequence);
        while (seqReader.Read())
        {
            seqTokens.Add(SyntaxTokenInfo.FromReader(seqReader));
        }

        // assert
        Assert.Equal(spanTokens.Count, seqTokens.Count);
        for (var i = 0; i < spanTokens.Count; i++)
        {
            Assert.Equal(spanTokens[i].Kind, seqTokens[i].Kind);
            Assert.Equal(spanTokens[i].Start, seqTokens[i].Start);
            Assert.Equal(spanTokens[i].End, seqTokens[i].End);
            Assert.Equal(spanTokens[i].Line, seqTokens[i].Line);
            Assert.Equal(spanTokens[i].Column, seqTokens[i].Column);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(13)]
    [InlineData(64)]
    [InlineData(128)]
    public void MultiSegment_ChunkSizes_Match_Span(int chunkSize)
    {
        // arrange
        var sourceText = "{ x { y } }"u8.ToArray();
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, chunkSize);

        // act - read with span (reference)
        var spanTokens = new List<SyntaxTokenInfo>();
        var spanReader = new Utf8GraphQLReader(sourceText.AsSpan());
        while (spanReader.Read())
        {
            spanTokens.Add(SyntaxTokenInfo.FromReader(spanReader));
        }

        // act - read with multi-segment sequence
        var seqTokens = new List<SyntaxTokenInfo>();
        var seqReader = new Utf8GraphQLReader(sequence);
        while (seqReader.Read())
        {
            seqTokens.Add(SyntaxTokenInfo.FromReader(seqReader));
        }

        // assert
        Assert.Equal(spanTokens.Count, seqTokens.Count);
        for (var i = 0; i < spanTokens.Count; i++)
        {
            Assert.Equal(spanTokens[i].Kind, seqTokens[i].Kind);
            Assert.Equal(spanTokens[i].Start, seqTokens[i].Start);
            Assert.Equal(spanTokens[i].End, seqTokens[i].End);
            Assert.Equal(spanTokens[i].Line, seqTokens[i].Line);
            Assert.Equal(spanTokens[i].Column, seqTokens[i].Column);
        }
    }

    [Fact]
    public void MultiSegment_SplitInMiddleOfName()
    {
        // arrange - "type foo" with split in "fo|o"
        var sourceText = "type foo"u8.ToArray();
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, [6]); // "type f" | "oo"

        // act
        var tokens = new List<SyntaxTokenInfo>();
        var reader = new Utf8GraphQLReader(sequence);
        while (reader.Read())
        {
            tokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenKind.Name, tokens[0].Kind);
        Assert.Equal(TokenKind.Name, tokens[1].Kind);
    }

    [Fact]
    public void MultiSegment_SplitInMiddleOfString()
    {
        // arrange
        var sourceText = """{ me(a: "hello world") }"""u8.ToArray();
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, [12]); // split inside "hello"

        // act
        var tokens = new List<SyntaxTokenInfo>();
        var reader = new Utf8GraphQLReader(sequence);
        while (reader.Read())
        {
            tokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert - should parse successfully
        Assert.Contains(tokens, t => t.Kind == TokenKind.String);
    }

    [Fact]
    public void MultiSegment_SplitInMiddleOfNumber()
    {
        // arrange - "{ x(a: 12345) }" split in middle of number
        var sourceText = "{ x(a: 12345) }"u8.ToArray();
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, [9]); // "{ x(a: 1" | "2345) }"

        // act
        var tokens = new List<SyntaxTokenInfo>();
        var reader = new Utf8GraphQLReader(sequence);
        while (reader.Read())
        {
            tokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        Assert.Contains(tokens, t => t.Kind == TokenKind.Integer);
    }

    [Fact]
    public void MultiSegment_SplitAtPunctuator()
    {
        // arrange
        var sourceText = "{ x { y } }"u8.ToArray();
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, [4]); // "{ x " | "{ y } }"

        // act
        var tokens = new List<SyntaxTokenInfo>();
        var reader = new Utf8GraphQLReader(sequence);
        while (reader.Read())
        {
            tokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        var spanTokens = new List<SyntaxTokenInfo>();
        var spanReader = new Utf8GraphQLReader(sourceText.AsSpan());
        while (spanReader.Read())
        {
            spanTokens.Add(SyntaxTokenInfo.FromReader(spanReader));
        }

        Assert.Equal(spanTokens.Count, tokens.Count);
    }

    [Fact]
    public void MultiSegment_SplitInSpread()
    {
        // arrange - "{ ...fragName }" split in the middle of "..."
        var sourceText = "{ ...fragName }"u8.ToArray();
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, [3]); // "{ ." | "..fragName }"

        // act
        var tokens = new List<SyntaxTokenInfo>();
        var reader = new Utf8GraphQLReader(sequence);
        while (reader.Read())
        {
            tokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        Assert.Contains(tokens, t => t.Kind == TokenKind.Spread);
    }

    [Fact]
    public void MultiSegment_SplitInBlockString()
    {
        // arrange
        var sourceText = "{ me(a: \"\"\"\n     Abcdef\n\"\"\") }"u8.ToArray();
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, 5);

        // act
        var tokens = new List<SyntaxTokenInfo>();
        var reader = new Utf8GraphQLReader(sequence);
        while (reader.Read())
        {
            tokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        Assert.Contains(tokens, t => t.Kind == TokenKind.BlockString);
    }

    [Fact]
    public void MultiSegment_SplitInFloat()
    {
        // arrange - split in middle of float
        var sourceText = "{ x(a: 3.14) }"u8.ToArray();
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, [9]); // split at "3." | "14) }"

        // act
        var tokens = new List<SyntaxTokenInfo>();
        var reader = new Utf8GraphQLReader(sequence);
        while (reader.Read())
        {
            tokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        Assert.Contains(tokens, t => t.Kind == TokenKind.Float);
    }

    [Fact]
    public void MultiSegment_SplitInComment()
    {
        // arrange
        var sourceText = "{ #test me foo bar \n me }"u8.ToArray();
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, 7);

        // act
        var tokens = new List<SyntaxTokenInfo>();
        var reader = new Utf8GraphQLReader(sequence);
        while (reader.Read())
        {
            tokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        Assert.Contains(tokens, t => t.Kind == TokenKind.Comment);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(16)]
    [InlineData(64)]
    public void MultiSegment_KitchenSinkQuery_MatchesSpan(int chunkSize)
    {
        // arrange
        var sourceText = Encoding.UTF8.GetBytes(
            FileResource.Open("kitchen-sink.graphql")
                .NormalizeLineBreaks());
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, chunkSize);

        // act - read with span (reference)
        var spanTokens = new List<SyntaxTokenInfo>();
        var spanReader = new Utf8GraphQLReader(sourceText.AsSpan());
        while (spanReader.Read())
        {
            spanTokens.Add(SyntaxTokenInfo.FromReader(spanReader));
        }

        // act - read with multi-segment sequence
        var seqTokens = new List<SyntaxTokenInfo>();
        var seqReader = new Utf8GraphQLReader(sequence);
        while (seqReader.Read())
        {
            seqTokens.Add(SyntaxTokenInfo.FromReader(seqReader));
        }

        // assert
        Assert.Equal(spanTokens.Count, seqTokens.Count);
        for (var i = 0; i < spanTokens.Count; i++)
        {
            Assert.Equal(spanTokens[i].Kind, seqTokens[i].Kind);
            Assert.Equal(spanTokens[i].Start, seqTokens[i].Start);
            Assert.Equal(spanTokens[i].End, seqTokens[i].End);
            Assert.Equal(spanTokens[i].Line, seqTokens[i].Line);
            Assert.Equal(spanTokens[i].Column, seqTokens[i].Column);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(16)]
    [InlineData(64)]
    public void MultiSegment_Parser_KitchenSinkQuery_ProducesIdenticalAST(int chunkSize)
    {
        // arrange
        var sourceText = Encoding.UTF8.GetBytes(
            FileResource.Open("kitchen-sink.graphql")
                .NormalizeLineBreaks());
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, chunkSize);

        // act - parse with span
        var spanDoc = Utf8GraphQLParser.Parse(sourceText.AsSpan());

        // act - parse with sequence
        var seqDoc = Utf8GraphQLParser.Parse(sequence);

        // assert - both produce identical SDL output
        Assert.Equal(spanDoc.ToString(), seqDoc.ToString());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(16)]
    [InlineData(64)]
    public void MultiSegment_Parser_KitchenSinkSchema_ProducesIdenticalAST(int chunkSize)
    {
        // arrange
        var sourceText = Encoding.UTF8.GetBytes(
            FileResource.Open("schema-kitchen-sink.graphql")
                .NormalizeLineBreaks());
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, chunkSize);

        // act
        var spanDoc = Utf8GraphQLParser.Parse(sourceText.AsSpan());
        var seqDoc = Utf8GraphQLParser.Parse(sequence);

        // assert
        Assert.Equal(spanDoc.ToString(), seqDoc.ToString());
    }

    [Fact]
    public void MultiSegment_VerySmallSegments_OneByteEach()
    {
        // arrange - every byte in its own segment
        var sourceText = "{ x }"u8.ToArray();
        var sequence = TestSequenceSegment.CreateMultiSegment(sourceText, 1);

        // act
        var tokens = new List<SyntaxTokenInfo>();
        var reader = new Utf8GraphQLReader(sequence);
        while (reader.Read())
        {
            tokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        Assert.Equal(3, tokens.Count); // { x }
        Assert.Equal(TokenKind.LeftBrace, tokens[0].Kind);
        Assert.Equal(TokenKind.Name, tokens[1].Kind);
        Assert.Equal(TokenKind.RightBrace, tokens[2].Kind);
    }

    [Fact]
    public void MultiSegment_SingleSegment_Sequence_Parser()
    {
        // arrange
        var sourceText = "{ x { y } }"u8.ToArray();
        var sequence = new ReadOnlySequence<byte>(sourceText);

        // act
        var doc = Utf8GraphQLParser.Parse(sequence);

        // assert
        Assert.NotNull(doc);
        Assert.Single(doc.Definitions);
    }

    [Fact]
    public void MultiSegment_EmptySequence_Throws()
    {
        // arrange
        var sequence = new ReadOnlySequence<byte>(Array.Empty<byte>());

        // act & assert
        Assert.Throws<ArgumentException>(() => new Utf8GraphQLReader(sequence));
    }
}
