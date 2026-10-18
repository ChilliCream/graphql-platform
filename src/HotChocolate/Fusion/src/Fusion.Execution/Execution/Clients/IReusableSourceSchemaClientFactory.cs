namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// A source schema client factory whose clients hold no request state and can therefore
/// serve consecutive requests of a pooled <see cref="DefaultSourceSchemaClientScope"/>.
/// </summary>
internal interface IReusableSourceSchemaClientFactory
{
    /// <summary>
    /// Gets how long after its creation a client for the given configuration may be reused.
    /// <see cref="TimeSpan.Zero"/> means the client serves a single request.
    /// </summary>
    /// <param name="configuration">The client configuration.</param>
    /// <returns>The maximum age of a reused client.</returns>
    TimeSpan GetMaxClientAge(ISourceSchemaClientConfiguration configuration);
}
