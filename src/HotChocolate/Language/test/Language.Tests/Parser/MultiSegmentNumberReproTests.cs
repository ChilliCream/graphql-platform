using System.Buffers;
using System.Text;

namespace HotChocolate.Language;

public class MultiSegmentNumberReproTests
{
    private const string CustomerQuery =
        """
        query Organizations($cursor: String) {
          organizations(first: 100, after: $cursor) {
            pageInfo {
              hasNextPage
              endCursor
              __typename
            }
            edges {
              node {
                ...Organization
                __typename
              }
              __typename
            }
            __typename
          }
        }

        fragment Organization on Organization {
          id
          Number
          displayName
          __typename
        }
        """;

    [Fact]
    public void Parse_CustomerQuery_AllChunkSizes_DoNotCorruptNumber()
    {
        // arrange
        var data = Encoding.UTF8.GetBytes(CustomerQuery);
        var failures = new List<string>();

        // act
        for (var chunkSize = 1; chunkSize <= data.Length; chunkSize++)
        {
            var sequence = TestSequenceSegment.CreateMultiSegment(data, chunkSize);

            try
            {
                var reader = new Utf8GraphQLReader(sequence);
                while (reader.Read())
                {
                    if (reader.Kind == TokenKind.Integer)
                    {
                        var value = Encoding.UTF8.GetString(reader.Value);
                        if (value != "100")
                        {
                            failures.Add($"chunkSize={chunkSize}: integer token = '{value}' (expected '100')");
                        }
                    }
                }
            }
            catch (SyntaxException ex)
            {
                failures.Add($"chunkSize={chunkSize}: {ex.Message}");
            }
        }

        // assert
        Assert.True(
            failures.Count == 0,
            "Multi-segment reader corrupted the number token:\n" + string.Join("\n", failures));
    }
}
