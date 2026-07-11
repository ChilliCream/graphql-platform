using Demo.Billing.Data;
using Demo.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;

namespace Demo.Billing.Queries;

public record GetInvoicesQuery : IQuery<List<Invoice>>;

public class GetInvoicesQueryHandler(BillingDbContext db)
    : IQueryHandler<GetInvoicesQuery, List<Invoice>>
{
    public async ValueTask<List<Invoice>> HandleAsync(
        GetInvoicesQuery query, CancellationToken cancellationToken)
        => await db.Invoices.Include(i => i.Payments).ToListAsync(cancellationToken);
}

public record GetInvoiceByIdQuery(Guid Id) : IQuery<Invoice?>;

public class GetInvoiceByIdQueryHandler(BillingDbContext db)
    : IQueryHandler<GetInvoiceByIdQuery, Invoice?>
{
    public async ValueTask<Invoice?> HandleAsync(
        GetInvoiceByIdQuery query, CancellationToken cancellationToken)
        => await db.Invoices.Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == query.Id, cancellationToken);
}

public record GetInvoiceByOrderIdQuery(Guid OrderId) : IQuery<Invoice?>;

public class GetInvoiceByOrderIdQueryHandler(BillingDbContext db)
    : IQueryHandler<GetInvoiceByOrderIdQuery, Invoice?>
{
    public async ValueTask<Invoice?> HandleAsync(
        GetInvoiceByOrderIdQuery query, CancellationToken cancellationToken)
        => await db.Invoices.Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.OrderId == query.OrderId, cancellationToken);
}
