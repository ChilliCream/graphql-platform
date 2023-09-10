namespace HotChocolate.Fusion.CommandLine.Helpers;

internal record SubgraphFiles(
    string SchemaFile,
    string SubgraphConfigFile,
    IReadOnlyList<string> ExtensionFiles);
