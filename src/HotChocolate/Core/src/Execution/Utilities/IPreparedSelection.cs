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

        /// </summary>
        /// Defines if this selection needs post processing for skip and include.
        /// <summary>
        bool IsFinal { get; }

        bool IsVisible(IVariableValueCollection variables);
    }
}
