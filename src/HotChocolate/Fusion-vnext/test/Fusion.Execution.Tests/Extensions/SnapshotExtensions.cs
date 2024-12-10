using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Planning.Nodes;

namespace HotChocolate.Fusion;

public static class TestSnapshotExtensions
{
    public static void MatchSnapshot(this RequestPlanNode plan)
    {
        plan.ToYaml().MatchSnapshot(".yaml");
    }
}
