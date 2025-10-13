namespace HotChocolate.ModelContextProtocol.Diagnostics;

/// <summary>
/// Provides diagnostic events for the Model Context Protocol (MCP) integration.
/// </summary>
public interface IMcpDiagnosticEvents
{
    /// <summary>
    /// Called when the MCP tools are being initialized.
    /// </summary>
    /// <returns>
    /// Returns a scope that is disposed when the initialization is complete.
    /// </returns>
    IDisposable InitializeTools();

    /// <summary>
    /// Called when the MCP tools are being updated.
    /// </summary>
    /// <returns>
    /// Returns a scope that is disposed when the update is complete.
    /// </returns>
    IDisposable UpdateTools();

    /// <summary>
    /// Called when errors occur while validating a tool document.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    void ValidationErrors(IReadOnlyList<IError> errors);
}
