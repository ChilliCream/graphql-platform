using System.Collections.Immutable;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Features;

internal sealed class SourceInputFieldMetadata
{
    public bool HasEventCursorDirective { get; set; }

    public bool HasIsDirective { get; set; }

    public bool HasRequireDirective { get; set; }

    public bool IsInaccessible { get; set; }

    public IsInfo? IsInfo { get; set; }

    public RequireInfo? RequireInfo { get; set; }
}

internal sealed record IsInfo(Directive Directive)
{
    public ImmutableArray<string> ContextNames { get; set; } = [];

    public bool IsInvalidFieldType { get; set; }

    public bool IsInvalidFieldSyntax { get; set; }
}

internal sealed record RequireInfo(Directive Directive)
{
    public ImmutableArray<string> ContextNames { get; set; } = [];

    public bool IsInvalidFieldType { get; set; }

    public bool IsInvalidFieldSyntax { get; set; }
}
