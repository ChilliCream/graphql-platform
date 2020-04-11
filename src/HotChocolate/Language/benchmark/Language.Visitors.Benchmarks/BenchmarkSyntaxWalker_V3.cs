using HotChocolate.Language.Visitors;

namespace HotChocolate.Language.Visitors.Benchmarks
{
    public class BenchmarkSyntaxWalker_V3 : SyntaxWalker
    {
        public BenchmarkSyntaxWalker_V3()
            : base(Continue)
        {
        }
    }
}
