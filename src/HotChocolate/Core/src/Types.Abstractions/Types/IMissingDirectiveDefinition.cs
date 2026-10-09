namespace HotChocolate.Types;

/// <summary>
/// Marks a directive definition that is missing from the schema. When a schema document is
/// parsed that applies a directive without a definition, the parser creates a missing directive
/// definition to plug the hole. These can later be replaced by the actual definition, and until
/// then are reported as undefined by schema validation.
/// </summary>
public interface IMissingDirectiveDefinition : IDirectiveDefinition;
