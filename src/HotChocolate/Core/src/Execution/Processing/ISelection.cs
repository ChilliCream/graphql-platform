using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// Represents a field selection during execution.
    /// </summary>
    public interface ISelection
        : IFieldSelection
        , IOptionalSelection
    {
        /// <summary>
        /// Gets an operation unique identifier of this selection.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the execution kind.
        /// </summary>
        SelectionExecutionStrategy Strategy { get; }

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
        FieldDelegate? ResolverPipeline { get; }

        /// <summary>
        /// The compiled pure resolver.
        /// </summary>
        PureFieldDelegate? PureResolver { get; }

        /// <summary>
        /// The compiled inline resolver that can be used to optimize execution of a request.
        /// </summary>
        InlineFieldDelegate? InlineResolver { get; }

        /// <summary>
        /// The arguments that have been pre-coerced for this field selection.
        /// </summary>
        IArgumentMap Arguments { get; }
    }
}
