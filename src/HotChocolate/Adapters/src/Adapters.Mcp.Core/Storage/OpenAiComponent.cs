using System.Diagnostics.CodeAnalysis;
using static HotChocolate.Adapters.Mcp.Properties.McpAdapterResources;

namespace HotChocolate.Adapters.Mcp.Storage;

public sealed class OpenAiComponent([StringSyntax("HTML")] string htmlTemplateText)
{
    /// <summary>
    /// HTML template (<c>text/html+skybridge</c>).
    /// </summary>
    public string HtmlTemplateText { get; } = htmlTemplateText;

    /// <summary>
    /// Define connect_domains and resource_domains arrays for the component's CSP snapshot.
    /// </summary>
    public OpenAiComponentCsp? ContentSecurityPolicy { get; init; }

    /// <summary>
    /// Human-readable summary surfaced to the model when the component loads, reducing redundant
    /// assistant narration.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional dedicated subdomain for hosted components (defaults to
    /// <c>https://web-sandbox.oaiusercontent.com</c>).
    /// </summary>
    public string? Domain { get; init; }

    /// <summary>
    /// Hint that the component should render inside a bordered card when supported.
    /// </summary>
    public bool? PrefersBorder { get; init; }

    /// <summary>
    /// Short status text while the tool runs (≤ 64 chars).
    /// </summary>
    public string? ToolInvokingStatusText
    {
        get;
        init
        {
            if (value?.Length > 64)
            {
                throw new ArgumentException(OpenAiComponent_ToolInvokingStatusTextCannotExceed64Characters);
            }

            field = value;
        }
    }

    /// <summary>
    /// Short status text after the tool completes ≤ 64 chars.
    /// </summary>
    public string? ToolInvokedStatusText
    {
        get;
        init
        {
            if (value?.Length > 64)
            {
                throw new ArgumentException(OpenAiComponent_ToolInvokedStatusTextCannotExceed64Characters);
            }

            field = value;
        }
    }

    /// <summary>
    /// Allow component→tool calls through the client bridge.
    /// </summary>
    public bool AllowToolCalls { get; init; }
}
