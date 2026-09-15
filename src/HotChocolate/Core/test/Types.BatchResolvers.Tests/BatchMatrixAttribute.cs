using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace HotChocolate.Types.BatchResolvers;

[AttributeUsage(AttributeTargets.Method)]
public sealed class BatchMatrixAttribute : DataAttribute
{
    public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
        MethodInfo testMethod,
        DisposalTracker disposalTracker)
        => new(Enum.GetValues<DeclarationStyle>()
            .Select(style => (ITheoryDataRow)new TheoryDataRow<DeclarationStyle>(style))
            .ToArray());

    public override bool SupportsDiscoveryEnumeration() => true;
}
