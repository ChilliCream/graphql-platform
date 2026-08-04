using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data;

internal sealed class DataTypeInterceptor : TypeInterceptor
{
    private readonly Dictionary<Type, HashSet<string>> _objectFieldIgnores = [];
    private readonly List<SortInputTypeConfiguration> _sortConfigurations = [];
    private readonly List<FilterInputTypeConfiguration> _filterConfigurations = [];

    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
        switch (configuration)
        {
            case ObjectTypeConfiguration objectTypeConfiguration:
                CollectObjectFieldIgnores(objectTypeConfiguration);
                break;

            case SortInputTypeConfiguration sortTypeConfiguration:
                _sortConfigurations.Add(sortTypeConfiguration);
                break;

            case FilterInputTypeConfiguration filterTypeConfiguration:
                _filterConfigurations.Add(filterTypeConfiguration);
                break;
        }
    }

    private void CollectObjectFieldIgnores(ObjectTypeConfiguration configuration)
    {
        var runtimeType = GetRuntimeType(configuration);
        if (runtimeType is null || configuration.FieldIgnores.Count == 0)
        {
            return;
        }

        if (!_objectFieldIgnores.TryGetValue(runtimeType, out var ignoredFields))
        {
            ignoredFields = new HashSet<string>(StringComparer.Ordinal);
            _objectFieldIgnores.Add(runtimeType, ignoredFields);
        }

        foreach (var ignore in configuration.FieldIgnores)
        {
            ignoredFields.Add(ignore.Name);
        }
    }

    public override void OnTypesInitialized()
    {
        foreach (var sortConfiguration in _sortConfigurations)
        {
            ApplySortFieldIgnores(sortConfiguration);
        }

        foreach (var filterConfiguration in _filterConfigurations)
        {
            ApplyFilterFieldIgnores(filterConfiguration);
        }
    }

    private void ApplySortFieldIgnores(SortInputTypeConfiguration configuration)
    {
        if (configuration.EntityType is not { } entityType
            || !_objectFieldIgnores.TryGetValue(entityType, out var ignoredFields))
        {
            return;
        }

        foreach (var possibleField in configuration.Fields)
        {
            if (possibleField is not SortFieldConfiguration field)
            {
                continue;
            }

            if (field.IsImplicit && ignoredFields.Contains(field.Name))
            {
                field.Ignore = true;
            }
        }
    }

    private void ApplyFilterFieldIgnores(FilterInputTypeConfiguration configuration)
    {
        if (configuration.EntityType is not { } entityType
            || !_objectFieldIgnores.TryGetValue(entityType, out var ignoredFields))
        {
            return;
        }

        foreach (var field in configuration.Fields.OfType<FilterFieldConfiguration>())
        {
            if (field.IsImplicit && ignoredFields.Contains(field.Name))
            {
                field.Ignore = true;
            }
        }
    }

    private static Type? GetRuntimeType(ObjectTypeConfiguration configuration)
    {
        if (configuration.RuntimeType != typeof(object))
        {
            return configuration.RuntimeType;
        }

        if (configuration.FieldBindingType is { } fieldBindingType
            && fieldBindingType != typeof(object))
        {
            return fieldBindingType;
        }

        return null;
    }
}
