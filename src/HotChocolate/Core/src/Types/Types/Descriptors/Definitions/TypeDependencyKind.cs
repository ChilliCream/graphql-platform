namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// Defines when the type dependency has to be fulfilled.
    /// </summary>
    public enum TypeDependencyKind
    {
        /// <summary>
        /// The dependency instance does not be completed.
        /// </summary>
        Default,

        /// <summary>
        /// The dependency instance needs to have it`s name completed.
        /// </summary>
        Named,

        /// <summary>
        /// The dependency instance needs to be fully completed.
        /// </summary>
        Completed
    }
}
