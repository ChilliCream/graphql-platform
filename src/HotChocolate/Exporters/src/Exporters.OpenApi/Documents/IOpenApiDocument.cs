namespace HotChocolate.Exporters.OpenApi;

public interface IOpenApiDocument
{
    string Id { get; }

    string Name { get; }

    string? Description { get; }

    IReadOnlyList<string> FragmentDependencies { get; }
}
