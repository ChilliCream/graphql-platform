using HotChocolate.Types;

namespace HotChocolate.Fusion.Directives;

internal sealed class McpToolAnnotationsDirective(
    bool? destructiveHint = null,
    bool? idempotentHint = null,
    bool? openWorldHint = null)
{
    public bool? DestructiveHint { get; set; } = destructiveHint;

    public bool? IdempotentHint { get; set; } = idempotentHint;

    public bool? OpenWorldHint { get; set; } = openWorldHint;

    public static McpToolAnnotationsDirective From(IDirective directive)
    {
        var destructiveHint =
            (bool?)directive.Arguments.GetValueOrDefault(WellKnownArgumentNames.DestructiveHint)?.Value;
        var idempotentHint =
            (bool?)directive.Arguments.GetValueOrDefault(WellKnownArgumentNames.IdempotentHint)?.Value;
        var openWorldHint =
            (bool?)directive.Arguments.GetValueOrDefault(WellKnownArgumentNames.OpenWorldHint)?.Value;

        return new McpToolAnnotationsDirective(destructiveHint, idempotentHint, openWorldHint);
    }
}
