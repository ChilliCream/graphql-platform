using HotChocolate.Types;

namespace HotChocolate.Data.Types.StatementTransactions;

[InterfaceType]
public abstract class StatementTransaction
{
    public int Id { get; init; }
}
