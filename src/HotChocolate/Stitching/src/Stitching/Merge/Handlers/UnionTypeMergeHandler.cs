namespace HotChocolate.Stitching.Merge.Handlers;

internal class UnionTypeMergeHandler : ITypeMergeHandler
{
    private readonly MergeTypeRuleDelegate _next;

    public UnionTypeMergeHandler(MergeTypeRuleDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public void Merge(
        ISchemaMergeContext context,
        IReadOnlyList<ITypeInfo> types)
    {
        if (types.OfType<UnionTypeInfo>().Any())
        {
            var notMerged = types.OfType<UnionTypeInfo>().ToList();
            var hasLeftovers = types.Count > notMerged.Count;

            for (var i = 0; i < notMerged.Count; i++)
            {
                context.AddType(notMerged[i].Definition.Rename(
                    TypeMergeHelpers.CreateName(context, notMerged[i]),
                    notMerged[i].Schema.Name));
            }

            if (hasLeftovers)
            {
                _next.Invoke(context, types.NotOfType<UnionTypeInfo>());
            }
        }
        else
        {
            _next.Invoke(context, types);
        }
    }
}
