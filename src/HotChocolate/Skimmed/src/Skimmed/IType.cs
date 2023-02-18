namespace HotChocolate.Skimmed;

public interface IType : IHasDirectives
{
    /// <summary>
    /// Gets the type kind.
    /// </summary>
    TypeKind Kind { get; }
}
