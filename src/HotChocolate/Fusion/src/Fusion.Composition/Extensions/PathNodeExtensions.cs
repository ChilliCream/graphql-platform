using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Satisfiability;

namespace HotChocolate.Fusion.Extensions;

internal static class PathNodeExtensions
{
    extension(PathNode? path)
    {
        public bool ContainsItem(SatisfiabilityPathItem item)
        {
            for (var node = path; node is not null; node = node.Parent)
            {
                if (node.Item == item)
                {
                    return true;
                }
            }

            return false;
        }

        public string ToPathString()
        {
            if (path is null)
            {
                return string.Empty;
            }

            var items = new List<SatisfiabilityPathItem>();

            for (var node = path; node is not null; node = node.Parent)
            {
                items.Add(node.Item);
            }

            items.Reverse();

            return string.Join(" -> ", items);
        }
    }
}
