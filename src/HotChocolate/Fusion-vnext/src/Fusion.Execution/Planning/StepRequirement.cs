using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public record StepRequirement(
    string Key,
    int StepId,
    ITypeNode TypeNode);
