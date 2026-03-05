using Demo.Catalog.Sagas;
using Demo.Contracts.Commands;
using Demo.Contracts.Events;
using Mocha.Sagas.Tests;

namespace Demo.Catalog.Tests.Sagas;

public sealed class ReturnProcessingSagaTests
{
    private static readonly Guid OrderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ReturnId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid RefundId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private const int Quantity = 2;
    private const decimal Amount = 99.99m;
    private const string CustomerId = "CUST-002";
    private const string Reason = "Defective product";
    private const string TrackingNumber = "TRK-12345";

    private static ReturnPackageReceivedEvent CreatePackageReceivedEvent()
        => new()
        {
            ReturnId = ReturnId,
            OrderId = OrderId,
            TrackingNumber = TrackingNumber,
            ReceivedAt = DateTimeOffset.UtcNow,
            ProductId = ProductId,
            Quantity = Quantity,
            Amount = Amount,
            CustomerId = CustomerId,
            Reason = Reason
        };

    private static InspectReturnResponse CreateInspectionResponse(InspectionResult result = InspectionResult.Passed)
        => new()
        {
            OrderId = OrderId,
            ProductId = ProductId,
            ReturnId = ReturnId,
            Passed = result == InspectionResult.Passed,
            Result = result,
            InspectedAt = DateTimeOffset.UtcNow
        };

    private static RestockInventoryResponse CreateRestockResponse()
        => new()
        {
            OrderId = OrderId,
            ProductId = ProductId,
            QuantityRestocked = Quantity,
            NewStockLevel = 50,
            Success = true,
            RestockedAt = DateTimeOffset.UtcNow
        };

    private static ProcessRefundResponse CreateRefundSuccessResponse()
        => new()
        {
            RefundId = RefundId,
            OrderId = OrderId,
            Amount = Amount,
            Success = true,
            ProcessedAt = DateTimeOffset.UtcNow
        };

    private static ProcessRefundResponse CreateRefundFailureResponse()
        => new()
        {
            RefundId = Guid.Empty,
            OrderId = OrderId,
            Amount = 0,
            Success = false,
            FailureReason = "Payment gateway unavailable",
            ProcessedAt = DateTimeOffset.UtcNow
        };

    [Fact]
    public async Task Should_TransitionToAwaitingInspection_And_SendInspectCommand_When_PackageReceived()
    {
        var tester = SagaTester.Create(new ReturnProcessingSaga());

        await tester
            .Plan()
            .On(CreatePackageReceivedEvent())
            .ExpectState("AwaitingInspection")
            .ExpectSendMessage<InspectReturnCommand>(
                (state, cmd) =>
                {
                    Assert.Equal(OrderId, cmd.OrderId);
                    Assert.Equal(ProductId, cmd.ProductId);
                    Assert.Equal(Quantity, cmd.Quantity);
                    Assert.Equal(ReturnId, cmd.ReturnId);
                })
            .RunAll();
    }

    [Fact]
    public async Task Should_SendRestockAndRefund_When_InspectionCompletes()
    {
        var tester = SagaTester.Create(new ReturnProcessingSaga());

        await tester
            .Plan()
            .On(CreatePackageReceivedEvent())
            .ExpectState("AwaitingInspection")
            .ThenOn(CreateInspectionResponse())
            .ExpectState("AwaitingBothReplies")
            .ExpectSendMessage<RestockInventoryCommand>(
                (state, cmd) =>
                {
                    Assert.Equal(OrderId, cmd.OrderId);
                    Assert.Equal(ProductId, cmd.ProductId);
                    Assert.Equal(Quantity, cmd.Quantity);
                    Assert.Equal(ReturnId, cmd.ReturnId);
                })
            .ExpectSendMessage<ProcessRefundCommand>(
                (state, cmd) =>
                {
                    Assert.Equal(OrderId, cmd.OrderId);
                    Assert.Equal(Amount, cmd.Amount);
                    Assert.Equal(CustomerId, cmd.CustomerId);
                })
            .RunAll();
    }

    [Fact]
    public async Task Should_Complete_When_RestockArrivesFirst_ThenRefund()
    {
        var tester = SagaTester.Create(new ReturnProcessingSaga());

        await tester
            .Plan()
            .On(CreatePackageReceivedEvent())
            .ThenOn(CreateInspectionResponse())
            .ThenOn(CreateRestockResponse())
            .ExpectState("RestockDoneAwaitingRefund")
            .ThenOn(CreateRefundSuccessResponse())
            .ExpectState("Completed")
            .ExpectCompletion()
            .RunAll();

        // Verify final state values before cleanup
        // State is cleaned up after Finally, so we check outbox instead
        var refundCmd = tester.ExpectSentMessage<ProcessRefundCommand>();
        Assert.Equal(OrderId, refundCmd.OrderId);
    }

    [Fact]
    public async Task Should_Complete_When_RefundArrivesFirst_ThenRestock()
    {
        var tester = SagaTester.Create(new ReturnProcessingSaga());

        await tester
            .Plan()
            .On(CreatePackageReceivedEvent())
            .ThenOn(CreateInspectionResponse())
            .ThenOn(CreateRefundSuccessResponse())
            .ExpectState("RefundDoneAwaitingRestock")
            .ThenOn(CreateRestockResponse())
            .ExpectState("Completed")
            .ExpectCompletion()
            .RunAll();
    }

    [Fact]
    public async Task Should_PopulateStateFromEvent_When_StateFactoryCreates()
    {
        var tester = SagaTester.Create(new ReturnProcessingSaga());

        await tester.Plan().On(CreatePackageReceivedEvent()).RunAll();

        var state = tester.State!;
        Assert.Equal(OrderId, state.OrderId);
        Assert.Equal(ProductId, state.ProductId);
        Assert.Equal(Quantity, state.Quantity);
        Assert.Equal(ReturnId, state.ReturnId);
        Assert.Equal(TrackingNumber, state.ReturnTrackingNumber);
        Assert.Equal(CustomerId, state.CustomerId);
        Assert.Equal(Amount, state.Amount);
        Assert.Equal(Reason, state.Reason);
    }

    [Fact]
    public async Task Should_SetFailureReason_When_RefundFails_DuringParallelPhase()
    {
        var tester = SagaTester.Create(new ReturnProcessingSaga());

        await tester
            .Plan()
            .On(CreatePackageReceivedEvent())
            .ThenOn(CreateInspectionResponse())
            .ThenOn(CreateRefundFailureResponse())
            .ExpectState("RefundDoneAwaitingRestock")
            .RunAll();

        var state = tester.State!;
        Assert.Equal("Payment gateway unavailable", state.FailureReason);
    }

    [Fact]
    public async Task Should_StoreInspectionResult_When_InspectionResponds()
    {
        var tester = SagaTester.Create(new ReturnProcessingSaga());

        await tester
            .Plan()
            .On(CreatePackageReceivedEvent())
            .ThenOn(CreateInspectionResponse(InspectionResult.Defective))
            .RunAll();

        var state = tester.State!;
        Assert.Equal(InspectionResult.Defective, state.InspectionResult);
    }
}
