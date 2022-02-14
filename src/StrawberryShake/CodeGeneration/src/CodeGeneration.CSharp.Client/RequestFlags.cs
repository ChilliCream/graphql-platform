namespace StrawberryShake.CodeGeneration.CSharp;

public enum RequestOptions
{
    Default = 0,
    ExportPersistedQueries = 1,
    ExportPersistedQueriesJson = 2,
    GenerateRazorComponent = 4,
    GenerateCSharpClient = 8,
}
