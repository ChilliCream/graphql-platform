using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Keeps the relative cursor fields out of the schema unless relative cursors are enabled globally
/// or on at least one field.
/// </summary>
internal sealed class PageInfoRelativeCursorFieldsTypeInterceptor : TypeInterceptor
{
    private readonly List<(ObjectTypeConfiguration Type, int Index, ObjectFieldConfiguration Field)>
        _removedFields = [];
    private bool _fieldEnabled;

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
        switch (configuration)
        {
            case ObjectTypeConfiguration typeConfiguration:
                _fieldEnabled |= typeConfiguration.Fields.Any(t => IsEnabled(t.GetFeatures()));

                if (!IsEnabled(discoveryContext.DescriptorContext.Features))
                {
                    RemoveRelativeCursorFields(typeConfiguration);
                }

                break;

            case InterfaceTypeConfiguration interfaceConfiguration:
                _fieldEnabled |= interfaceConfiguration.Fields.Any(t => IsEnabled(t.GetFeatures()));
                break;
        }
    }

    public override IEnumerable<TypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
    {
        if (_removedFields.Count == 0 || !_fieldEnabled)
        {
            yield break;
        }

        var restored = _removedFields.ToArray();
        _removedFields.Clear();

        foreach (var (typeConfiguration, index, field) in restored)
        {
            typeConfiguration.Fields.Insert(index, field);

            if (field.Type is { } type)
            {
                yield return type;
            }
        }
    }

    private void RemoveRelativeCursorFields(ObjectTypeConfiguration typeConfiguration)
    {
        var removed = new List<(ObjectTypeConfiguration Type, int Index, ObjectFieldConfiguration Field)>();

        for (var i = typeConfiguration.Fields.Count - 1; i >= 0; i--)
        {
            var field = typeConfiguration.Fields[i];

            if (field.Flags.HasFlag(CoreFieldFlags.RelativeCursorField))
            {
                removed.Insert(0, (typeConfiguration, i, field));
                typeConfiguration.Fields.RemoveAt(i);
            }
        }

        _removedFields.AddRange(removed);
    }

    private static bool IsEnabled(IFeatureCollection features)
        => features.TryGet<PagingOptions>(out var options) && options.EnableRelativeCursors is true;
}
