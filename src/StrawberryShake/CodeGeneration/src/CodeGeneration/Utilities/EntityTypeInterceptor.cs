using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace StrawberryShake.CodeGeneration.Utilities;

internal sealed class EntityTypeInterceptor : TypeInterceptor
{
    private readonly List<TypeInfo> _outputTypes = [];
    private readonly IReadOnlyList<SelectionSetNode> _globalEntityPatterns;
    private readonly IReadOnlyDictionary<string, SelectionSetNode> _typeEntityPatterns;

    public EntityTypeInterceptor(
        IReadOnlyList<SelectionSetNode> globalEntityPatterns,
        IReadOnlyDictionary<string, SelectionSetNode> typeEntityPatterns)
    {
        _globalEntityPatterns = globalEntityPatterns;
        _typeEntityPatterns = typeEntityPatterns;
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (completionContext.Type is IComplexTypeDefinition outputType)
        {
            if (_typeEntityPatterns.TryGetValue(outputType.Name, out var pattern))
            {
                configuration.Features.Set(new EntityFeature(pattern));
            }
            else
            {
                _outputTypes.Add(new TypeInfo(outputType, configuration.Features));
            }
        }
    }

    public override void OnAfterCompleteTypes()
    {
        if (_globalEntityPatterns.Count > 0)
        {
            foreach (var typeInfo in _outputTypes)
            {
                if (_globalEntityPatterns.FirstOrDefault(
                    pattern => DoesPatternMatch(typeInfo.Type, pattern)) is { } matchedPattern)
                {
                    typeInfo.Features.Set(new EntityFeature(matchedPattern));
                }
            }
        }
    }

    private bool DoesPatternMatch(IComplexTypeDefinition outputType, SelectionSetNode pattern)
    {
        // TODO : At the moment we just allow the first level.

        foreach (var selection in pattern.Selections.OfType<FieldNode>())
        {
            if (selection.SelectionSet is null
                && outputType.Fields.TryGetField(selection.Name.Value, out var field)
                && field.Type.NamedType().IsLeafType())
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private readonly struct TypeInfo(
        IComplexTypeDefinition type,
        IFeatureCollection features)
        : IFeatureProvider
    {
        public IComplexTypeDefinition Type { get; } = type;

        public IFeatureCollection Features { get; } = features;
    }
}
