using HotChocolate.Language;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents a variable definition used in an EntityResolver.
/// </summary>
/// <param name="Name">
/// The name of the variable.
/// </param>
/// <param name="Field">
/// The field from which the data for this variable is retrieved from.
/// </param>
/// <param name="Definition">
/// The variable definition node.
/// </param>
internal sealed record VariableDefinition(
    string Name,
    FieldNode Field,
    VariableDefinitionNode Definition);
