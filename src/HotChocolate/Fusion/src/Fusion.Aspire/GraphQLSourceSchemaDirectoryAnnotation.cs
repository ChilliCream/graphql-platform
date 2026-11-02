using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// The directory that the schema composition reads the schema settings and schema files of a
/// resource from. It replaces the project directory of a resource that carries project
/// metadata.
/// </summary>
internal sealed class GraphQLSourceSchemaDirectoryAnnotation(string directory) : IResourceAnnotation
{
    public string Directory { get; } = directory;
}
