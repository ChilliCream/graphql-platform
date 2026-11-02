namespace HotChocolate.Fusion.Suites.Mutations.A;

/// <summary>
/// Input shape for <c>Mutation.addProduct</c>.
/// </summary>
public sealed record AddProductInput(string Name, double Price);
