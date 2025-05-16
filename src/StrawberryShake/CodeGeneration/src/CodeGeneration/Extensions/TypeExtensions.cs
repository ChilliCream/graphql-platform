using HotChocolate.Language;
using HotChocolate.Types;
using static StrawberryShake.CodeGeneration.Analyzers.WellKnownContextData;

namespace StrawberryShake.CodeGeneration.Extensions;

public static class TypeExtensions
{
    public static bool IsEntity(this ITypeDefinition typeDefinition)
        => typeDefinition.ContextData.ContainsKey(Entity);

    public static SelectionSetNode GetEntityDefinition(this ITypeDefinition typeDefinition)
        => (SelectionSetNode)typeDefinition.ContextData[Entity]!;

    public static string GetRuntimeType(this ILeafType leafType)
        => (string)leafType.ContextData[RuntimeType]!;

    public static string GetSerializationType(this ILeafType leafType)
        => (string)leafType.ContextData[SerializationType]!;
}
