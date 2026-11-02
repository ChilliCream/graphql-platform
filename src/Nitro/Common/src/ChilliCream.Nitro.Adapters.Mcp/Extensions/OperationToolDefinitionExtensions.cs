using System.Collections.Immutable;
using ChilliCream.Nitro.Adapters.Mcp.Serialization;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Language;

namespace ChilliCream.Nitro.Adapters.Mcp.Extensions;

public static class OperationToolDefinitionExtensions
{
    extension(OperationToolDefinition operationToolDefinition)
    {
        public static OperationToolDefinition From(
            DocumentNode document,
            string name,
            McpToolSettings? settings,
            string? viewHtml)
        {
            return new OperationToolDefinition(document)
            {
                Name = name,
                Title = settings?.Title,
                Icons =
                    settings?.Icons?.Select(
                        i => new IconDefinition(i.Source)
                        {
                            MimeType = i.MimeType,
                            Sizes = i.Sizes,
                            Theme = i.Theme
                        }).ToImmutableArray(),
                DestructiveHint = settings?.Annotations?.DestructiveHint,
                IdempotentHint = settings?.Annotations?.IdempotentHint,
                OpenWorldHint = settings?.Annotations?.OpenWorldHint,
                View = viewHtml is null ? null : new McpAppView(viewHtml)
                {
                    Csp = settings?.View?.Csp is { } csp
                        ? new McpAppViewCsp
                        {
                            BaseUriDomains = csp.BaseUriDomains?.ToImmutableArray(),
                            ConnectDomains = csp.ConnectDomains?.ToImmutableArray(),
                            FrameDomains = csp.FrameDomains?.ToImmutableArray(),
                            ResourceDomains = csp.ResourceDomains?.ToImmutableArray()
                        }
                        : null,
                    Domain = settings?.View?.Domain,
                    Permissions = settings?.View?.Permissions is { } permissions
                        ? new McpAppViewPermissions
                        {
                            Camera = permissions.Camera,
                            ClipboardWrite = permissions.ClipboardWrite,
                            Geolocation = permissions.Geolocation,
                            Microphone = permissions.Microphone
                        }
                        : null,
                    PrefersBorder = settings?.View?.PrefersBorder
                },
                Visibility = settings?.Visibility is { } visibility
                    ? visibility.ToImmutableArray()
                    : null
            };
        }
    }
}
