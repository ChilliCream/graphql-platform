using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    /// Represents a field selection during execution.
    /// </summary>
    public interface IPreparedSelection : IFieldSelection
    {
        /// <summary>
        /// The type that declares the field that is selected by this selection.
        /// </summary>
        ObjectType DeclaringType { get; }

        /// <summary>
        /// If this selection selects a field that returns a composite type 
        /// then this selection set represents the fields that are selected 
        /// on that returning composit type.
        /// 
        /// If this selection however selects a leaf field than this 
        /// selection set will be <c>null</c>.
        /// </summary>
        SelectionSetNode? SelectionSet { get; }

        /// <summary>
        /// The compiled resolver pipeline for this selection.
        /// </summary>
        FieldDelegate ResolverPipeline { get; }

        /// <summary>
        /// The arguments that have been pre-coerced for this field selection.
        /// </summary>
        IPreparedArgumentMap Arguments { get; }

        /// </summary>
        /// Defines if this selection needs post processing for skip and include.
        /// <summary>
        bool IsFinal { get; }

        /// <summary>
        /// Defines if this field is visible with the following set of variables. 
        /// If <see cref="IsFinal" /> is <c>true</c> this method will allways return true.
        /// </summary>
        /// <param name="variables">
        /// The variable values of the execution context.
        /// </param>
        /// <returns>
        /// Return <c>true</c> if this selection is visible with the current set of variables;
        /// otherwise, <c>false</c> is returned.
        /// </returns>
        bool IsVisible(IVariableValueCollection variables);
    }
}
