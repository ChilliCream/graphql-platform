using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    public interface IPreparedSelection : IFieldSelection
    {
        ObjectType DeclaringType { get; }

        SelectionSetNode? SelectionSet { get; }

        FieldDelegate ResolverPipeline { get; }

        IPreparedArgumentMap Arguments { get; }

        bool IsFinal { get; }

        bool IsVisible(IVariableValueCollection variables);
    }
}
