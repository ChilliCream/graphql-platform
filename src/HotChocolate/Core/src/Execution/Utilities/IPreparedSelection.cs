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
        IObjectType DeclaringType { get; }

        /// <summary>
        /// If this selection selects a field that returns a composite type
        /// then this selection set represents the fields that are selected
        /// on that returning composite type.
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

        /// <summary>
        /// Defines when this selection is included for processing.
        /// </summary>
        public SelectionInclusionKind InclusionKind { get; }

        /// <summary>
        /// Defines that this selection is only needed for internal processing.
        /// </summary>
        public bool IsInternal { get; }

        /// <summary>
        /// Defines that this selection is conditional and will not always be included.
        /// </summary>
        public bool IsConditional { get; }

        /// <summary>
        /// Defines if this field is included into the selection set with the following
        /// set of <paramref name="variableValues"/>.
        /// If <see cref="InclusionKind" /> is <see cref="SelectionInclusionKind.Always"/>
        /// this method will always return true.
        /// </summary>
        /// <param name="variableValues">
        /// The variable values of the execution context.
        /// </param>
        /// <param name="allowInternals">
        /// Allow internal selections to be marked as included.
        /// </param>
        /// <returns>
        /// Return <c>true</c> if this selection is visible with the current set of variables;
        /// otherwise, <c>false</c> is returned.
        /// </returns>
        bool IsIncluded(IVariableValueCollection variableValues, bool allowInternals = false);
    }
}
