namespace HotChocolate.Fusion.Execution.Clients;

[Flags]
public enum SourceSchemaHttpClientBatchingMode
{
    VariableBatching = 1,
    RequestBatching = 2,
    ApolloRequestBatching = 4
}
