namespace HotChocolate.Types;

public interface IReadOnlyFieldDefinition
{
    string Name { get; }

    string? Description { get; }

    IReadOnlyTypeDefinition Type { get; }

    bool IsDeprecated { get; }

    string? DeprecationReason { get; }

    IReadOnlyDirectiveCollection Directives { get; }
}
