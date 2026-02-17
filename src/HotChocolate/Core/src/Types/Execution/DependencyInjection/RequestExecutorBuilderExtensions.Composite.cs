using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Applies recommended defaults for source schemas following the Composite Schema Specification.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <returns>
    /// Returns the request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    public static IRequestExecutorBuilder AddSourceSchemaDefaults(
        this IRequestExecutorBuilder builder)
        => builder.ModifyOptions(o =>
        {
            o.ApplyShareableToConnections = true;
            o.ApplyShareableToPageInfo = true;
            o.ApplyShareableToNodeFields = true;
            o.ApplySerializeAsToScalars = true;
        });
}
