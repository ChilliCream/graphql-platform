namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// Defines when a selection is included for processing.
    /// </summary>
    public enum SelectionInclusionKind
    {
        /// <summary>
        /// The selection is always included for processing.
        /// </summary>
        Always,

        /// <summary>
        /// The selection is only included if certain conditions are met.
        /// </summary>
        Conditional,

        /// <summary>
        /// The selection is only included for internal processing and
        /// will not appear in the result set.
        /// </summary>
        Internal,

        /// <summary>
        /// The selection is included for internal processing when certain
        /// conditions are met and will not appear in the result set.
        /// </summary>
        InternalConditional
    }
}
