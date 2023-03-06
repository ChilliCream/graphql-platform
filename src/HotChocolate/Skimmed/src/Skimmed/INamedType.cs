namespace HotChocolate.Skimmed;

public interface INamedType : IType, IHasName, IHasDirectives, IHasContextData
{
    /// <summary>
    /// Gets the description of the field.
    /// </summary>
    string? Description { get; set; }
}
