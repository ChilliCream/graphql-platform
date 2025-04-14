using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal static class InternalExtensions
{
    public static PlanStep? ById(this ImmutableList<PlanStep> allSteps, int id)
    {
        id--;
        if(id > 0 && id < allSteps.Count)
        {
            return allSteps[id];
        }

        return null;
    }
}
