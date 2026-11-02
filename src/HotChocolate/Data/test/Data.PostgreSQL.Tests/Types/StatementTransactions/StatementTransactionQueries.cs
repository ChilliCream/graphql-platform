using HotChocolate.Types;

namespace HotChocolate.Data.Types.StatementTransactions;

[QueryType]
public static partial class StatementTransactionQueries
{
    public static StatementTransaction GetStatementTransaction()
        => new DepositStatementTransaction
        {
            Id = 1,
            CollectionAmount = 42
        };
}
