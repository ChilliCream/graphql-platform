using Demo.Billing.Data;
using Demo.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;

namespace Demo.Billing.Queries;

public record GetRefundsQuery : IQuery<List<Refund>>;

public class GetRefundsQueryHandler(BillingDbContext db)
    : IQueryHandler<GetRefundsQuery, List<Refund>>
{
    public async ValueTask<List<Refund>> HandleAsync(
        GetRefundsQuery query, CancellationToken cancellationToken)
        => await db.Refunds.ToListAsync(cancellationToken);
}

public record GetRefundsByOrderIdQuery(Guid OrderId) : IQuery<List<Refund>>;

public class GetRefundsByOrderIdQueryHandler(BillingDbContext db)
    : IQueryHandler<GetRefundsByOrderIdQuery, List<Refund>>
{
    public async ValueTask<List<Refund>> HandleAsync(
        GetRefundsByOrderIdQuery query, CancellationToken cancellationToken)
        => await db.Refunds.Where(r => r.OrderId == query.OrderId).ToListAsync(cancellationToken);
}
