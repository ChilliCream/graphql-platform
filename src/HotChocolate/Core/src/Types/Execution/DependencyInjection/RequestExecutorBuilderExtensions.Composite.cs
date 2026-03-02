using HotChocolate;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Applies recommended defaults for source schemas following the Composite Schema Specification.
    /// </summary>
    /// <remarks>
    /// This method registers the schema as a source schema, which automatically enables
    /// both variable batching and request batching on the server options.
    /// Batching is required for the gateway to efficiently fetch data from source schemas.
    /// </remarks>
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
    {
        builder.Services.AddSingleton(new SourceSchemaRegistration(builder.Name));

        return builder.ModifyOptions(o =>
        {
            o.ApplyShareableToConnections = true;
            o.ApplyShareableToPageInfo = true;
            o.ApplyShareableToNodeFields = true;
            o.ApplySerializeAsToScalars = true;
        });
    }
}
