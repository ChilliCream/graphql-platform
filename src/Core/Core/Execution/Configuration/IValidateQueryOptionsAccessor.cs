namespace HotChocolate.Execution.Configuration
{
    /// <summary>
    /// Represents a dedicated options accessor to read the validation query
    /// configuration.
    /// </summary>
    public interface IValidateQueryOptionsAccessor
    {
        /// <summary>
        /// Gets the maximum allowed depth of a query. The default value is
        /// <see langword="null"/>. The minimum allowed value is <c>1</c>.
        /// </summary>
        int? MaxExecutionDepth { get; }

        int? MaxOperationComplexity { get; }

        bool? UseComplexityMultipliers { get; }
    }
}
