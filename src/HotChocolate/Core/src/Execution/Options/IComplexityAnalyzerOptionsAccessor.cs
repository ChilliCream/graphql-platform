namespace HotChocolate.Execution.Options
{
    /// <summary>
    /// The complexity options accessor.
    /// </summary>
    public interface IComplexityAnalyzerOptionsAccessor
    {
        /// <summary>
        /// Gets the complexity analyzer settings.
        /// </summary>
        ComplexityAnalyzerSettings Complexity { get; }
    }
}
