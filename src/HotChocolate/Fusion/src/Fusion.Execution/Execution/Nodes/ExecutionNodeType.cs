namespace HotChocolate.Fusion.Execution.Nodes;

public enum ExecutionNodeType
{
    Operation,
    OperationBatch,
    EventStream,
    FieldError,
    Introspection,
    Node
}
