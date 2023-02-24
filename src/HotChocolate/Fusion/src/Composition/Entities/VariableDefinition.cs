using HotChocolate.Language;

namespace HotChocolate.Fusion.Composition;

public sealed record VariableDefinition(
    string Name,
    FieldNode Field,
    VariableDefinitionNode Definition);
