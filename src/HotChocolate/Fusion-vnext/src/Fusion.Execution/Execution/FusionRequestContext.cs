using HotChocolate.Execution;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents a Fusion GraphQL request context.
/// </summary>
public sealed class FusionRequestContext : RequestContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="FusionRequestContext"/> with the specified
    /// </summary>
    /// <param name="schema">
    /// The GraphQL schema definition against which the request is executed.
    /// </param>
    /// <param name="request">
    /// The GraphQL request definition.
    /// </param>
    /// <param name="requestServices">
    /// The GraphQL request service provider.
    /// </param>
    /// <param name="executorVersion">
    /// The executor version.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="schema"/>, <paramref name="request"/> or
    /// <paramref name="requestServices"/> is <c>null</c>.
    /// </exception>
    public FusionRequestContext(
        FusionSchemaDefinition schema,
        IOperationRequest request,
        IServiceProvider requestServices,
        ulong executorVersion)
        : base(schema, request, requestServices, executorVersion)
    {
        Schema = schema;
    }

    /// <summary>
    /// Gets the GraphQL schema definition against which the request is executed.
    /// </summary>
    public new FusionSchemaDefinition Schema { get; }
}
