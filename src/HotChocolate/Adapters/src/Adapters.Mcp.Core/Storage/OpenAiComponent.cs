using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using CaseConverter;
using static HotChocolate.Adapters.Mcp.Properties.McpAdapterResources;

namespace HotChocolate.Adapters.Mcp.Storage;

public sealed class OpenAiComponent
{
    public OpenAiComponent(string name, [StringSyntax("HTML")] string htmlTemplateText)
    {
        Name = name;
        HtmlTemplateText = htmlTemplateText;
    }

    public string Name { get; }

    /// <summary>
    /// HTML template (<c>text/html+skybridge</c>).
    /// </summary>
    [MemberNotNull(nameof(OutputTemplate))]
    public string HtmlTemplateText
    {
        get;
        private set
        {
            field = value;
            var name = Name.ToKebabCase();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
            OutputTemplate = $"ui://components/{name}-{hash}.html";
        }
    }

    /// <summary>
    /// Resource URI for component HTML template.
    /// </summary>
    public string OutputTemplate { get; private set; }

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
