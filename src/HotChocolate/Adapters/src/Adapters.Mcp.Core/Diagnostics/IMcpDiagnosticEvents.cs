namespace HotChocolate.Adapters.Mcp.Diagnostics;

/// <summary>
/// Provides diagnostic events for the Model Context Protocol (MCP) integration.
/// </summary>
public interface IMcpDiagnosticEvents
{
    /// <summary>
    /// Called when the MCP prompts are being initialized.
    /// </summary>
    /// <returns>
    /// Returns a scope that is disposed when the initialization is complete.
    /// </returns>
    IDisposable InitializePrompts();

    /// <summary>
    /// Called when the MCP prompts are being updated.
    /// </summary>
    /// <returns>
    /// Returns a scope that is disposed when the update is complete.
    /// </returns>
    IDisposable UpdatePrompts();

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
    /// Called when creating a tool from a validated tool document fails.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="exception">The exception that occurred.</param>
    void ToolCreationFailed(string toolName, Exception exception);

    /// <summary>
    /// Called when errors occur while validating a tool document.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    void ValidationErrors(IReadOnlyList<IError> errors);
}
