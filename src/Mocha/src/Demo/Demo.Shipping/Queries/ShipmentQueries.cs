using Demo.Shipping.Data;
using Demo.Shipping.Entities;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;

namespace Demo.Shipping.Queries;

public record GetShipmentsQuery : IQuery<List<Shipment>>;

public class GetShipmentsQueryHandler(ShippingDbContext db)
    : IQueryHandler<GetShipmentsQuery, List<Shipment>>
{
    public async ValueTask<List<Shipment>> HandleAsync(
        GetShipmentsQuery query, CancellationToken cancellationToken)
        => await db.Shipments.Include(s => s.Items).ToListAsync(cancellationToken);
}

public record GetShipmentByIdQuery(Guid Id) : IQuery<Shipment?>;

public class GetShipmentByIdQueryHandler(ShippingDbContext db)
    : IQueryHandler<GetShipmentByIdQuery, Shipment?>
{
    public async ValueTask<Shipment?> HandleAsync(
        GetShipmentByIdQuery query, CancellationToken cancellationToken)
        => await db.Shipments.Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken);
}

public record GetShipmentByOrderIdQuery(Guid OrderId) : IQuery<Shipment?>;

public class GetShipmentByOrderIdQueryHandler(ShippingDbContext db)
    : IQueryHandler<GetShipmentByOrderIdQuery, Shipment?>
{
    public async ValueTask<Shipment?> HandleAsync(
        GetShipmentByOrderIdQuery query, CancellationToken cancellationToken)
        => await db.Shipments.Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.OrderId == query.OrderId, cancellationToken);
}
