using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

internal readonly struct RootSelection
{
    public RootSelection(ISelection selection, FetchDefinition? resolver)
    {
        Selection = selection;
        Resolver = resolver;
    }

    public ISelection Selection { get; }

    public FetchDefinition? Resolver { get; }
}
