using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Handlers;

internal class ObjectTypeMergeHandler : TypeMergeHandlerBase<ObjectTypeInfo>
{
    public ObjectTypeMergeHandler(MergeTypeRuleDelegate next)
        : base(next)
    {
    }

    protected override void MergeTypes(
        ISchemaMergeContext context,
        IReadOnlyList<ObjectTypeInfo> types,
        string newTypeName)
    {
        var definitions = types
            .Select(t => t.Definition)
            .ToList();

        // ? : how do we handle the interfaces correctly
        var interfaces = new HashSet<string>(
            definitions.SelectMany(d =>
                d.Interfaces.Select(t => t.Name.Value)));

        var definition = definitions[0]
            .WithInterfaces(interfaces.Select(t =>
                new NamedTypeNode(new NameNode(t))).ToList())
            .Rename(newTypeName, types.Select(t => t.Schema.Name));

        context.AddType(definition);
    }

    protected override bool CanBeMerged(
        ObjectTypeInfo left, ObjectTypeInfo right) =>
        left.CanBeMergedWith(right);
}
