namespace HotChocolate.Execution.Configuration
{
    /// <summary>
    /// The resolver compiler builder.
    /// </summary>
    public interface IResolverCompilerBuilder
    {
        /// <summary>
        /// THe inner request executor builder.
        /// </summary>
        IRequestExecutorBuilder RequestExecutorBuilder { get; }
    }
}
