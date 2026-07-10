using Demo.Billing.Data;
using Demo.Billing.Entities;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;

namespace Demo.Billing.Queries;

public record GetRevenueSummariesQuery : IQuery<List<RevenueSummary>>;

public class GetRevenueSummariesQueryHandler(BillingDbContext db)
    : IQueryHandler<GetRevenueSummariesQuery, List<RevenueSummary>>
{
    public async ValueTask<List<RevenueSummary>> HandleAsync(
        GetRevenueSummariesQuery query, CancellationToken cancellationToken)
        => await db.RevenueSummaries.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
}

public record GetLatestRevenueSummaryQuery : IQuery<RevenueSummary?>;

public class GetLatestRevenueSummaryQueryHandler(BillingDbContext db)
    : IQueryHandler<GetLatestRevenueSummaryQuery, RevenueSummary?>
{
    public async ValueTask<RevenueSummary?> HandleAsync(
        GetLatestRevenueSummaryQuery query, CancellationToken cancellationToken)
        => await db.RevenueSummaries.OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
}
