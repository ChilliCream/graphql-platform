using Demo.Shipping.Data;
using Demo.Shipping.Entities;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;

namespace Demo.Shipping.Queries;

public record GetReturnShipmentsQuery : IQuery<List<ReturnShipment>>;

public class GetReturnShipmentsQueryHandler(ShippingDbContext db)
    : IQueryHandler<GetReturnShipmentsQuery, List<ReturnShipment>>
{
    public async ValueTask<List<ReturnShipment>> HandleAsync(
        GetReturnShipmentsQuery query, CancellationToken cancellationToken)
        => await db.ReturnShipments.ToListAsync(cancellationToken);
}

public record GetReturnShipmentByIdQuery(Guid Id) : IQuery<ReturnShipment?>;

public class GetReturnShipmentByIdQueryHandler(ShippingDbContext db)
    : IQueryHandler<GetReturnShipmentByIdQuery, ReturnShipment?>
{
    public async ValueTask<ReturnShipment?> HandleAsync(
        GetReturnShipmentByIdQuery query, CancellationToken cancellationToken)
        => await db.ReturnShipments.FirstOrDefaultAsync(
            r => r.Id == query.Id, cancellationToken);
}

public record GetReturnShipmentByOrderIdQuery(Guid OrderId) : IQuery<ReturnShipment?>;

public class GetReturnShipmentByOrderIdQueryHandler(ShippingDbContext db)
    : IQueryHandler<GetReturnShipmentByOrderIdQuery, ReturnShipment?>
{
    public async ValueTask<ReturnShipment?> HandleAsync(
        GetReturnShipmentByOrderIdQuery query, CancellationToken cancellationToken)
        => await db.ReturnShipments.FirstOrDefaultAsync(
            r => r.OrderId == query.OrderId, cancellationToken);
}
