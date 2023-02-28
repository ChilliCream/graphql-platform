namespace HotChocolate.Fusion.Composition;

internal static class ComposerFactory
{
    public static FusionGraphComposer CreateComposer()
        => new FusionGraphComposer(
            new[] { new RefResolverEntityEnricher() },
            new ITypeMergeHandler[]
            {
                new InterfaceTypeMergeHandler(),
                new UnionTypeMergeHandler(),
                new InputObjectTypeMergeHandler(),
                new EnumTypeMergeHandler(),
                new ScalarTypeMergeHandler()
            });
}
