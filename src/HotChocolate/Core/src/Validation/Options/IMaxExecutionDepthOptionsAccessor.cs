namespace HotChocolate.Validation.Options
{
    public interface IMaxExecutionDepthOptionsAccessor
    {
        /// <summary>
        /// Gets the maximum allowed depth of a query. The default value is
        /// <see langword="null"/>. The minimum allowed value is <c>1</c>.
        /// </summary>
        int? MaxAllowedExecutionDepth { get; }
    }
}
