namespace HotChocolate.Types;

public interface IReadOnlyFieldDefinition
{
    string Name { get; }

    IReadOnlyTypeDefinition Type { get; }

    bool IsDeprecated { get; }

    string? DeprecationReason { get; }

    IReadOnlyDirectiveCollection Directives { get; }
}
