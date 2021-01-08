using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public class FragmentNode
    {
        public FragmentNode(
            Fragment fragment, 
            IReadOnlyList<FragmentNode> nodes)
        {
            Fragment = fragment;
            Nodes = nodes;
        }

        public Fragment Fragment { get; }

        public IReadOnlyList<FragmentNode> Nodes { get; }
    }
}
