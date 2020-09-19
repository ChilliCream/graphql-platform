namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    /// Represents selections with inclusion conditions.
    /// </summary>
    public interface IOptionalSelection
    {
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
        /// Defines if this selection is included into the selection set with the following
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
