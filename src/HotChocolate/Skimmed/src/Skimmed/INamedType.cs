namespace HotChocolate.Skimmed;

public interface INamedType : IType, IHasName, IHasDirectives
{
    /// <summary>
    /// Gets the field name.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets the description of the field.
    /// </summary>
    string? Description { get; set; }

    IDictionary<string, object?> ContextData { get; }
}
