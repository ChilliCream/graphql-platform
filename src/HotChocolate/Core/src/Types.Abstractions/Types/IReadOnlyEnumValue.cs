namespace HotChocolate.Types;

public interface IReadOnlyEnumValue : ISyntaxNodeProvider
{
    string Name { get; }

    string? Description { get; }

    bool IsDeprecated { get; }

    string? DeprecationReason { get; }

    IReadOnlyDirectiveCollection Directives { get; }
}