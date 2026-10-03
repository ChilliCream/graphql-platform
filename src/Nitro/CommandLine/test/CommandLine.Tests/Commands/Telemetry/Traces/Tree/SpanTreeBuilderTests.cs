using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces.Tree;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Traces.Tree;

public sealed class SpanTreeBuilderTests
{
    [Fact]
    public void Build_Should_LinkChildren_When_ParentSpanIdMatches()
    {
        // arrange
        var spans = new[] { CreateSpan("child", "root"), CreateSpan("root"), CreateSpan("orphan", "missing") };

        // act
        var tree = SpanTreeBuilder.Build(spans);

        // assert
        Assert.Equal(2, tree.Roots.Count);
        Assert.Equal("root", tree.Roots[0].Span.SpanId);
        Assert.Equal("orphan", tree.Roots[1].Span.SpanId);
        Assert.Equal("child", Assert.Single(tree.Roots[0].Children).Span.SpanId);
    }

    private static TraceSpan CreateSpan(string id, string parent = "")
        => new(id, parent, id, "SERVER", 1, 0, "OK", string.Empty, [], [], [], null);
}
