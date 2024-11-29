using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using StrawberryShake.CodeGeneration.Analyzers;
using static StrawberryShake.CodeGeneration.Analyzers.WellKnownContextData;

namespace StrawberryShake.CodeGeneration.Utilities;

public class LeafTypeInterceptor : TypeInterceptor
{
    private readonly Dictionary<string, LeafTypeInfo> _scalarInfos;
    private readonly List<LeafType> _leafTypes = [];

    public LeafTypeInterceptor(Dictionary<string, LeafTypeInfo> scalarInfos)
    {
        _scalarInfos = scalarInfos ?? throw new ArgumentNullException(nameof(scalarInfos));
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (completionContext.Type is ILeafType leafType && definition is not null)
        {
            _leafTypes.Add(new LeafType(leafType, definition.ContextData));
        }
    }

    public override void OnAfterCompleteTypeNames()
    {
        foreach (var leafType in _leafTypes)
        {
            if (_scalarInfos.TryGetValue(leafType.Type.Name, out var scalarInfo))
            {
                if (leafType.Type is ScalarType)
                {
                    leafType.ContextData[RuntimeType] = scalarInfo.RuntimeType;
                }
                leafType.ContextData[SerializationType] = scalarInfo.SerializationType;
            }
            else
            {
                if (leafType.Type is ScalarType)
                {
                    leafType.ContextData[RuntimeType] = TypeNames.String;
                }
                leafType.ContextData[SerializationType] = TypeNames.String;
            }
        }
    }

    private readonly struct LeafType
    {
        public LeafType(ILeafType type, IDictionary<string, object?> contextData)
        {
            Type = type;
            ContextData = contextData;
        }

        public ILeafType Type { get; }

        public IDictionary<string, object?> ContextData { get; }
    }
}
