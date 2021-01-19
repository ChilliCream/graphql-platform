using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers;

namespace StrawberryShake.CodeGeneration.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsEntity(this INamedType namedType) =>
            namedType.ContextData.ContainsKey(WellKnownContextData.Entity);

        public static SelectionSetNode GetEntityDefinition(this INamedType namedType) => 
            (SelectionSetNode)namedType.ContextData[WellKnownContextData.Entity]!;
    }
}
