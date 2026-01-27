using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Types.DirectiveNames;

namespace HotChocolate.Fusion.Directives;

internal sealed class CostDirective(double weight)
{
    public double Weight { get; } = weight;

    public static CostDirective From(IDirective directive)
    {
        if (!directive.Arguments.TryGetValue(Cost.Arguments.Weight, out var weightArg)
            || weightArg is not StringValueNode name)
        {
            throw new InvalidOperationException(CostDirective_WeightArgument_Invalid);
        }

        return new CostDirective(double.Parse(name.Value));
    }
}
