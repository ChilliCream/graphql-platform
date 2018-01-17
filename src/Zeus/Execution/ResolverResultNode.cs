using System.Collections.Generic;

namespace Zeus.Execution
{
    public class ResolverResultNode
    {
        public ResolverResultNode(ResolverResult result)
        {
            Result = result;
        }

        public ResolverResult Result { get; }
        public ICollection<ResolverResultNode> Nodes { get; } = new List<ResolverResultNode>();
    }
}