using HotChocolate.Features;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    private sealed class EmptyFeatureProvider : IFeatureProvider
    {
        private EmptyFeatureProvider()
        {
        }

        public IFeatureCollection Features => FeatureCollection.Empty;

        public static EmptyFeatureProvider Instance { get; } = new();
    }
}
