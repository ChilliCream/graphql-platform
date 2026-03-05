namespace HotChocolate.Data.Types.StatementTransactions;

public sealed class DepositStatementTransaction : StatementTransaction
{
    public int CollectionAmount { get; init; }
}
