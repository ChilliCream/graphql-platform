using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public class FragmentNode
    {
        public FragmentNode(
            Fragment fragment,
            IReadOnlyList<FragmentNode>? nodes = null)
        {
            Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
            Nodes = nodes ?? Array.Empty<FragmentNode>();
        }

        public Fragment Fragment { get; }

        public IReadOnlyList<FragmentNode> Nodes { get; }

        public FragmentNode WithFragment(Fragment fragment)
        {
            if (fragment is null)
            {
                throw new ArgumentNullException(nameof(fragment));
            }

            return new FragmentNode(fragment, Nodes);
        }

        public FragmentNode WithNodes(IReadOnlyList<FragmentNode> nodes)
        {
            if (nodes is null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            return new FragmentNode(Fragment, nodes);
        }
    }
}
