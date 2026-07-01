using System.Text;

namespace HotChocolate.Language;

public class UnescapeNumberReproTests
{
    // JSON-escapes a byte sequence the way a GraphQL-over-HTTP client encodes the
    // "query" string (newline -> \n, quote -> \", backslash -> \\).
    private static byte[] JsonEscape(byte[] target)
    {
        var sb = new List<byte>(target.Length * 2);
        foreach (var b in target)
        {
            switch (b)
            {
                case (byte)'\n':
                    sb.Add((byte)'\\');
                    sb.Add((byte)'n');
                    break;
                case (byte)'\r':
                    sb.Add((byte)'\\');
                    sb.Add((byte)'r');
                    break;
                case (byte)'\t':
                    sb.Add((byte)'\\');
                    sb.Add((byte)'t');
                    break;
                case (byte)'"':
                    sb.Add((byte)'\\');
                    sb.Add((byte)'"');
                    break;
                case (byte)'\\':
                    sb.Add((byte)'\\');
                    sb.Add((byte)'\\');
                    break;
                default:
                    sb.Add(b);
                    break;
            }
        }

        return sb.ToArray();
    }

    private static byte[] Unescape(byte[] escaped)
    {
        var buffer = new byte[escaped.Length];
        var input = new ReadOnlySpan<byte>(escaped);
        var output = new Span<byte>(buffer);
        Utf8Helper.Unescape(in input, ref output, isBlockString: false);
        return output.ToArray();
    }

    [Fact]
    public void Unescape_SingleNewline_AtEveryPosition_AndLength_RoundTrips()
    {
        // arrange / act / assert
        var failures = new List<string>();

        // cross the SIMD 16/32-byte boundaries
        for (var length = 1; length <= 140; length++)
        {
            for (var newlinePos = 0; newlinePos < length; newlinePos++)
            {
                var target = new byte[length];
                for (var i = 0; i < length; i++)
                {
                    // printable filler; newline at the chosen position
                    target[i] = i == newlinePos ? (byte)'\n' : (byte)('a' + (i % 26));
                }

                var escaped = JsonEscape(target);
                var actual = Unescape(escaped);

                if (!actual.AsSpan().SequenceEqual(target))
                {
                    failures.Add(
                        $"length={length} newlinePos={newlinePos}: "
                        + $"expected '{Encoding.UTF8.GetString(target)}' "
                        + $"got '{Encoding.UTF8.GetString(actual)}'");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures.Take(10)));
    }

    [Fact]
    public void Unescape_ManyEscapes_RandomLayouts_RoundTrip()
    {
        // arrange
        // deterministic LCG so failures are reproducible
        ulong state = 0x1234_5678_9abc_def0;
        int Next(int maxExclusive)
        {
            state = state * 6364136223846793005UL + 1442695040888963407UL;
            return (int)((state >> 33) % (ulong)maxExclusive);
        }

        var escapeTargets = new byte[] { (byte)'\n', (byte)'\r', (byte)'\t', (byte)'"', (byte)'\\' };
        var failures = new List<string>();

        // act / assert
        for (var iteration = 0; iteration < 20000 && failures.Count == 0; iteration++)
        {
            var length = 1 + Next(160);
            var target = new byte[length];
            for (var i = 0; i < length; i++)
            {
                // ~35% chance of an escapable byte, clustering escapes
                target[i] = Next(100) < 35
                    ? escapeTargets[Next(escapeTargets.Length)]
                    : (byte)('a' + Next(26));
            }

            var escaped = JsonEscape(target);
            var actual = Unescape(escaped);

            if (!actual.AsSpan().SequenceEqual(target))
            {
                failures.Add(
                    $"iteration={iteration} length={length}\n"
                    + $"  target =[{string.Join(",", target.Select(b => (int)b))}]\n"
                    + $"  actual =[{string.Join(",", actual.Select(b => (int)b))}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void Unescape_CustomerQuery_RoundTrips()
    {
        // arrange
        const string query =
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

        var target = Encoding.UTF8.GetBytes(query);
        var escaped = JsonEscape(target);

        // act
        var actual = Unescape(escaped);

        // assert
        Assert.Equal(query, Encoding.UTF8.GetString(actual));
    }
}
