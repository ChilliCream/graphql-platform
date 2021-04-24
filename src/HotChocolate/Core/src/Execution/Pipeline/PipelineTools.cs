namespace HotChocolate.Execution.Pipeline
{
    internal static class PipelineTools
    {
        public static string CreateOperationId(string documentId, string? operationName) =>
            operationName is null ? documentId : $"{documentId}+{operationName}";
    }
}
