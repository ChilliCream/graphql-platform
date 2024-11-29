using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using WellKnownContextData = StrawberryShake.CodeGeneration.Analyzers.WellKnownContextData;

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
        DefinitionBase definition)
    {
        if (completionContext.Type is IComplexOutputType outputType &&
            definition is not null)
        {
            if (_typeEntityPatterns.TryGetValue(outputType.Name, out var pattern))
            {
                definition.ContextData[WellKnownContextData.Entity] = pattern;
            }
            else
            {
                _outputTypes.Add(new TypeInfo(outputType, definition.ContextData));
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
                    typeInfo.ContextData[WellKnownContextData.Entity] = matchedPattern;
                }
            }
        }
    }

    private bool DoesPatternMatch(IComplexOutputType outputType, SelectionSetNode pattern)
    {
        // TODO : At the moment we just allow the first level.

        foreach (var selection in pattern.Selections.OfType<FieldNode>())
        {
            if (selection.SelectionSet is null &&
                outputType.Fields.TryGetField(selection.Name.Value, out var field) &&
                field.Type.NamedType().IsLeafType())
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private readonly struct TypeInfo
    {
        public TypeInfo(IComplexOutputType type, IDictionary<string, object?> contextData)
        {
            Type = type;
            ContextData = contextData;
        }

        public IComplexOutputType Type { get; }

        public IDictionary<string, object?> ContextData { get; }
    }
}
