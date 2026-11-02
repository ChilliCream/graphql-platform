namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Provides access to the environment variables that configure the Nitro integration.
/// </summary>
internal interface INitroEnvironment
{
    /// <summary>
    /// Gets the value of an environment variable.
    /// </summary>
    /// <param name="name">
    /// The name of the environment variable.
    /// </param>
    /// <returns>
    /// The value of the environment variable, or <c>null</c> when it is not set.
    /// </returns>
    string? GetVariable(string name);
}
