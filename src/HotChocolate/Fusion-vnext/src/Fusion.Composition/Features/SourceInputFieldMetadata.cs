using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Features;

internal sealed class SourceInputFieldMetadata
{
    public bool HasIsDirective { get; set; }

    public bool HasRequireDirective { get; set; }

    public bool IsInaccessible { get; set; }

    public IsInfo? IsInfo { get; set; }

    public RequireInfo? RequireInfo { get; set; }
}

internal sealed record IsInfo(Directive Directive)
{
    public bool IsInvalidFieldType { get; set; }

    public bool IsInvalidFieldSyntax { get; set; }
}

internal sealed record RequireInfo(Directive Directive)
{
    public bool IsInvalidFieldType { get; set; }

    public bool IsInvalidFieldSyntax { get; set; }
}
