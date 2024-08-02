using HotChocolate.Language;
using HotChocolate.Types;
using static StrawberryShake.CodeGeneration.Analyzers.WellKnownContextData;

namespace StrawberryShake.CodeGeneration.Extensions;

public static class TypeExtensions
{
    public static bool IsEntity(this INamedType namedType) =>
        namedType.ContextData.ContainsKey(Entity);

    public static SelectionSetNode GetEntityDefinition(this INamedType namedType) =>
        (SelectionSetNode)namedType.ContextData[Entity]!;

    public static string GetRuntimeType(this ILeafType leafType) =>
        (string)leafType.ContextData[RuntimeType]!;

    public static string GetSerializationType(this ILeafType leafType) =>
        (string)leafType.ContextData[SerializationType]!;
}
