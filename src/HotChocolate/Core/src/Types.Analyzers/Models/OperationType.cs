namespace HotChocolate.Types.Analyzers.Models;

[Flags]
public enum OperationType
{
    No = 0,
    Query = 1,
    Mutation = 2,
    Subscription = 4
}
