using Demo.Billing.Data;
using Demo.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;

namespace Demo.Billing.Queries;

public record GetPaymentsQuery : IQuery<List<Payment>>;

public class GetPaymentsQueryHandler(BillingDbContext db)
    : IQueryHandler<GetPaymentsQuery, List<Payment>>
{
    public async ValueTask<List<Payment>> HandleAsync(
        GetPaymentsQuery query, CancellationToken cancellationToken)
        => await db.Payments.Include(p => p.Invoice).ToListAsync(cancellationToken);
}
