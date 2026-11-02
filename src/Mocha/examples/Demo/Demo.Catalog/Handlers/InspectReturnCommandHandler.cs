using Demo.Catalog.Data;
using Demo.Contracts.Commands;
using Microsoft.EntityFrameworkCore;
using Mocha;

namespace Demo.Catalog.Handlers;

public class InspectReturnCommandHandler(CatalogDbContext db, ILogger<InspectReturnCommandHandler> logger)
    : IEventRequestHandler<InspectReturnCommand, InspectReturnResponse>
{
    public async ValueTask<InspectReturnResponse> HandleAsync(
        InspectReturnCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Inspecting return {ReturnId} for order {OrderId}, product {ProductId}",
            request.ReturnId,
            request.OrderId,
            request.ProductId);

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            logger.LogWarning("Product {ProductId} not found during inspection", request.ProductId);
            return new InspectReturnResponse
            {
                OrderId = request.OrderId,
                ProductId = request.ProductId,
                ReturnId = request.ReturnId,
                Passed = false,
                Result = InspectionResult.WrongItem,
                Notes = "Product not found in catalog",
                InspectedAt = DateTimeOffset.UtcNow
            };
        }

        // Simulate inspection logic - in real world, this would involve
        // warehouse staff inspection, photos, condition assessment, etc.
        // For demo, we'll use a simple random decision weighted toward success
        var random = Random.Shared.NextDouble();

        InspectionResult result;
        bool passed;
        string notes;

        if (random < 0.75) // 75% pass rate
        {
            result = InspectionResult.Passed;
            passed = true;
            notes = "Item in good condition, suitable for restocking";
        }
        else if (random < 0.90) // 15% damaged by customer
        {
            result = InspectionResult.DamagedByCustomer;
            passed = false;
            notes = "Item shows signs of customer damage, partial refund recommended";
        }
        else if (random < 0.97) // 7% defective
        {
            result = InspectionResult.Defective;
            passed = true; // Defective items still get full refund
            notes = "Manufacturer defect identified, full refund authorized";
        }
        else // 3% wrong item
        {
            result = InspectionResult.WrongItem;
            passed = false;
            notes = "Returned item does not match order, investigation required";
        }

        logger.LogInformation(
            "Inspection complete for return {ReturnId}: Result={Result}, Passed={Passed}",
            request.ReturnId,
            result,
            passed);

        return new InspectReturnResponse
        {
            OrderId = request.OrderId,
            ProductId = request.ProductId,
            ReturnId = request.ReturnId,
            Passed = passed,
            Result = result,
            Notes = notes,
            InspectedAt = DateTimeOffset.UtcNow
        };
    }
}
