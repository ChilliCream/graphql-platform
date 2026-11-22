using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public partial class SourceResultDocumentTokenizerTests
{
    // The nodes of the shape that CreateUserShape creates.
    private const int ReviewNode = 0;
    private const int UserNode = 1;

    [Theory]
    [InlineData(
        """{"data":{"user":{"id":1,"name":"a","reviews":[{"body":"x","stars":5},{"body":"y","stars":4}]}}}""",
        "- - user review review")]
    [InlineData(
        """{ "data": { "user": { "id": 1, "name": "a", "reviews": [ { "body": "x", "stars": 5 } ] } } }""",
        "- - user review")]
    [InlineData(
        """{"data":{"user":{"id":1,"name":"a","reviews":[[{"body":"x","stars":5}],[]]}}}""",
        "- - user review")]
    [InlineData(
        """{"data":{"user":{"id":{"x":1},"name":"a","reviews":null}}}""",
        "- - user -")]
    [InlineData(
        """{"errors":[{"message":"m"}],"data":{"user":{"id":1,"name":"a","reviews":[]}}}""",
        "- - - user")]
    [InlineData(
        """{"data" :{"user" : {"id" :1,"name" :"a","reviews" : [{"body" : "x","stars" :5}]}}}""",
        "- - user review")]
    public void ParseFilled_Should_IdentifyObjects_When_TheyHaveTheExpectedPropertiesInOrder(
        string json,
        string expectedShapeIds)
    {
        // arrange
        var shape = CreateUserShape(anyRootField: false);

        // act
        var shapeIds = ParseShapeIds(json, shape);

        // assert
        Assert.Equal(expectedShapeIds, shapeIds);
    }

    [Theory]
    [InlineData("""{"data":{"user":{"id":1,"reviews":[{"body":"x","stars":5}]}}}""", "- - - review")]
    [InlineData("""{"data":{"user":{"name":"a","id":1,"reviews":[{"body":"x","stars":5}]}}}""", "- - - review")]
    [InlineData("""{"data":{"user":{"id":1,"name":"a","reviews":[],"extra":true}}}""", "- - -")]
    [InlineData("""{"data":{"user":{"id":1,"name":"a","reviews":[],"id":2}}}""", "- - -")]
    [InlineData("""{"data":{"user":{"id":1,"name":"a"}}}""", "- - -")]
    [InlineData("""{"data":{"user":{}}}""", "- - -")]
    [InlineData("""{"data":{"user":{"id":1,"name":"a","reviews":[{"stars":5,"body":"x"},{"body":"y","stars":4}]}}}""", "- - user - review")]
    public void ParseFilled_Should_NotIdentifyAnObject_When_ItsPropertiesAreNotTheExpectedOnes(
        string json,
        string expectedShapeIds)
    {
        // arrange
        var shape = CreateUserShape(anyRootField: false);

        // act
        var shapeIds = ParseShapeIds(json, shape);

        // assert
        Assert.Equal(expectedShapeIds, shapeIds);
    }

    [Fact]
    public void ParseFilled_Should_NotIdentifyAnObject_When_APropertyNameHasAnEscapeSequence()
    {
        // arrange
        var shape = CreateUserShape(anyRootField: false);

        // the first property of the user is the name id with its first letter as an escape sequence
        var json = "{\"data\":{\"user\":{\"" + (char)92 + "u0069d\":1,\"name\":\"a\",\"reviews\":[]}}}";

        // act
        var shapeIds = ParseShapeIds(json, shape);

        // assert
        Assert.Equal("- - -", shapeIds);
    }

    [Fact]
    public void ParseFilled_Should_IdentifyTheValueOfEveryRootField_When_RootFieldNamesAreNotExpected()
    {
        // arrange
        var shape = CreateUserShape(anyRootField: true);
        const string json =
            """
            {"data":{"a_0_user":{"id":1,"name":"a","reviews":[]},"a_1_user":null,"a_2_user":{"id":2,"name":"b","reviews":[{"body":"x","stars":1}]}}}
            """;

        // act
        var shapeIds = ParseShapeIds(json, shape);

        // assert
        Assert.Equal("- - user user review", shapeIds);
    }

    [Fact]
    public void ParseFilled_Should_NotIdentifyObjects_When_NoShapeIsExpected()
    {
        // arrange
        const string json = """{"data":{"user":{"id":1,"name":"a","reviews":[{"body":"x","stars":5}]}}}""";

        // act
        var shapeIds = ParseShapeIds(json, shape: null);

        // assert
        Assert.Equal("- - - -", shapeIds);
    }

    [Fact]
    public void ParseFilled_Should_IdentifyObjectsOrLeaveThemUnidentified_When_ChunkBoundariesMove()
    {
        // arrange
        var shape = CreateUserShape(anyRootField: false);
        var input = Encoding.UTF8.GetBytes(CreateUsersResponse(reviews: 40));
        var expected = ParseShapeIds(input, padding: 0, ChunkLayout.Adjacent, shape).Split(' ');
        var mismatches = new List<string>();
        var identified = 0;

        // act
        foreach (var padding in GetBoundaryPaddings(input.Length))
        {
            var padded = Pad(input, padding);

            foreach (var layout in Enum.GetValues<ChunkLayout>())
            {
                var shapeIds = ParseShapeIds(padded, padding, layout, shape).Split(' ');

                // An object whose property name lies on a chunk boundary is not identified.
                if (shapeIds.Length != expected.Length
                    || shapeIds.Where((id, i) => id != expected[i] && id != "-").Any())
                {
                    mismatches.Add($"{layout} {padding}");
                }

                identified += shapeIds.Count(id => id != "-");
            }
        }

        // assert
        Assert.Empty(mismatches);
        Assert.Equal(41, expected.Count(id => id != "-"));
        Assert.True(identified > 0);
    }

    [Fact]
    public void ParseFilled_Should_CreateTheRowsOfAParseWithoutShape_When_MutatedInputHasAnExpectedShape()
    {
        // arrange
        var shape = CreateUserShape(anyRootField: false);
        var anyRootFieldShape = CreateUserShape(anyRootField: true);
        byte[][] corpus =
        [
            Encoding.UTF8.GetBytes(CreateUsersResponse(reviews: 2)),
            Encoding.UTF8.GetBytes(CreateUsersResponse(reviews: 60)),
            """{"data":{"user":{"id":1,"name":"a","reviews":[{"body":"x","stars":5},{"stars":4,"body":"y"}]}}}"""u8.ToArray(),
            """{ "data" : { "user" : { "id" : 1 , "name" : "a" , "reviews" : [ ] } } }"""u8.ToArray(),
            """{"errors":[{"message":"m","path":["user"]}],"data":{"user":null}}"""u8.ToArray(),
            """{"data":{"a_0_user":{"id":1,"name":"a","reviews":[]},"a_1_user":{"name":"b","id":2}}}"""u8.ToArray()
        ];
        var random = new Random(61);
        var results = new StringBuilder();
        var expectedResults = new StringBuilder();

        // act
        for (var i = 0; i < 3000; i++)
        {
            var input = Mutate(random, corpus[random.Next(corpus.Length)]);
            var expected = ParseUnpadded(input);

            foreach (var padding in RandomBoundaryPaddings(random, input.Length))
            {
                var padded = Pad(input, padding);
                var layout = (ChunkLayout)random.Next(3);
                var expectedShape = random.Next(2) == 0 ? shape : anyRootFieldShape;

                results.Append($"{i} {layout} {padding}\n{ParseFilled(padded, padding, layout, expectedShape)}\n");
                expectedResults.Append($"{i} {layout} {padding}\n{expected}\n");
            }
        }

        // assert
        Assert.Equal(expectedResults.ToString(), results.ToString());
    }

    /// <summary>
    /// Creates the shape of a result whose <c>data</c> has a <c>user</c> with the properties
    /// <c>id</c>, <c>name</c> and <c>reviews</c>, where a review has a <c>body</c> and
    /// <c>stars</c>. With <paramref name="anyRootField"/> every property of <c>data</c> is a user.
    /// </summary>
    private static SourceResultShape CreateUserShape(bool anyRootField)
    {
        var builder = new SourceResultShape.Builder();
        var review = builder.AddObject(
            ["body", "stars"],
            [SourceResultShape.NoNode, SourceResultShape.NoNode],
            identify: true);
        var user = builder.AddObject(
            ["id", "name", "reviews"],
            [SourceResultShape.NoNode, SourceResultShape.NoNode, review],
            identify: true);
        var data = anyRootField
            ? builder.AddObjectOfAnyFields(user)
            : builder.AddObject(["user"], [user], identify: false);

        Assert.Equal((ReviewNode, UserNode), (review, user));
        return builder.Build(builder.AddObject(["data"], [data], identify: false));
    }

    private static string CreateUsersResponse(int reviews)
    {
        var text = new StringBuilder("""{"data":{"user":{"id":1,"name":"Ada Lovelace","reviews":[""");

        for (var i = 0; i < reviews; i++)
        {
            text.Append(i > 0 ? "," : "")
                .Append("{\"body\":\"The review with the number ")
                .Append(i)
                .Append(" has a long body.\",\"stars\":")
                .Append(i % 5)
                .Append('}');
        }

        return text.Append("]}}}").ToString();
    }

    private static string ParseShapeIds(string json, SourceResultShape? shape)
        => ParseShapeIds(Encoding.UTF8.GetBytes(json), padding: 0, ChunkLayout.Adjacent, shape);

    /// <summary>
    /// Parses <paramref name="input"/> and returns for every object in document order what it is
    /// identified as: <c>user</c>, <c>review</c> or <c>-</c> for an object that is not identified.
    /// </summary>
    private static string ParseShapeIds(byte[] input, int padding, ChunkLayout layout, SourceResultShape? shape)
    {
        using var arena = new MemoryArena();
        var (segments, usedChunks, lastLength) = CreateSegments(input, layout);

        try
        {
            using var document = SourceResultDocument.ParseFilled(arena, segments, usedChunks, lastLength, shape);
            var shapeIds = new List<string>();
            var cursor = SourceResultDocument.Cursor.Zero;
            var rowCount = document.GetDbRow(cursor).NumberOfRows;

            for (var i = 0; i < rowCount; i++, cursor++)
            {
                var row = document.GetDbRow(cursor);

                if (row.TokenType is JsonTokenType.StartObject)
                {
                    shapeIds.Add(
                        document.GetObjectShapeNode(cursor, row) switch
                        {
                            UserNode => "user",
                            ReviewNode => "review",
                            SourceResultShape.NoNode => "-",
                            var node => node.ToString()
                        });
                }
            }

            return string.Join(' ', shapeIds);
        }
        finally
        {
            arena.Seal();
        }
    }
}
