namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a directive definition that is missing from the schema. When a schema document is
/// parsed that applies a directive without a definition, the parser creates a missing directive
/// definition to plug the hole. These can later be replaced by the actual definition, and until
/// then are reported as undefined by schema validation.
/// </summary>
public sealed class MissingDirectiveDefinition : MutableDirectiveDefinition, IMissingDirectiveDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingDirectiveDefinition"/> class.
    /// </summary>
    /// <param name="name">
    /// The name of the directive that is applied but not defined.
    /// </param>
    public MissingDirectiveDefinition(string name) : base(name)
    {
        IsRepeatable = true;
        Locations = DirectiveLocation.TypeSystem;
    }
}
