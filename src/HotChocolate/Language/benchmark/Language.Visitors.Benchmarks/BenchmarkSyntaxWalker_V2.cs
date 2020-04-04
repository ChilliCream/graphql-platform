namespace HotChocolate.Language.Visitors.Benchmarks
{
    public class BenchmarkSyntaxWalker_V2 : SyntaxNodeVisitor
    {
        public BenchmarkSyntaxWalker_V2() : base(VisitorAction.Continue)
        {
        }
    }
}
