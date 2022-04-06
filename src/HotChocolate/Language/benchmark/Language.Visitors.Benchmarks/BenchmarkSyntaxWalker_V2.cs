namespace HotChocolate.Language.Visitors.Benchmarks;

public class BenchmarkSyntaxWalkerV2 : SyntaxNodeVisitor
{
    public BenchmarkSyntaxWalkerV2() : base(VisitorAction.Continue)
    {
    }
}
