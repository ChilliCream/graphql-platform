using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;
using StrawberryShake.CodeGeneration.Analyzers;

namespace StrawberryShake.CodeGeneration.Utilities;

public class LeafTypeInterceptor : TypeInterceptor
{
    private readonly Dictionary<string, LeafTypeInfo> _scalarInfos;
    private readonly List<LeafType> _leafTypes = [];

    public LeafTypeInterceptor(Dictionary<string, LeafTypeInfo> scalarInfos)
    {
        ArgumentNullException.ThrowIfNull(scalarInfos);

        _scalarInfos = scalarInfos;
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (completionContext.Type is ILeafType leafType)
        {
            _leafTypes.Add(new LeafType(leafType, configuration.Features));
        }
    }

    public override void OnAfterCompleteTypeNames()
    {
        foreach (var leafType in _leafTypes)
        {
            if (_scalarInfos.TryGetValue(leafType.Type.Name, out var scalarInfo))
            {
                leafType.Features.Set(leafType.Type is ScalarType
                    ? new LeafTypeFeature(scalarInfo.RuntimeType, scalarInfo.SerializationType)
                    : new LeafTypeFeature(null, scalarInfo.SerializationType));
            }
            else
            {
                leafType.Features.Set(leafType.Type is ScalarType
                    ? new LeafTypeFeature(TypeNames.String, TypeNames.String)
                    : new LeafTypeFeature(null, TypeNames.String));
            }
        }
    }

    private record LeafType(ILeafType Type, IFeatureCollection Features) : IFeatureProvider;
}
