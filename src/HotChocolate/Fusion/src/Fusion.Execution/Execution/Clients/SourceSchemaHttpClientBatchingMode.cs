namespace HotChocolate.Fusion.Execution.Clients;

[Flags]
public enum SourceSchemaHttpClientBatchingMode
{
    None = 0,
    VariableBatching = 1,
    RequestBatching = 2,
    ApolloRequestBatching = 4
}
