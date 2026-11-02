namespace HotChocolate.Data.Types.StatementTransactions;

public sealed class BillingStatementTransaction : StatementTransaction
{
    public int FeeAndChargeAmount { get; init; }
}
