using Demo.Catalog.Sagas;
using Demo.Contracts.Commands;
using Demo.Contracts.Saga;
using Mocha.Sagas.Tests;

namespace Demo.Catalog.Tests.Sagas;

public sealed class QuickRefundSagaTests
{
    private static readonly Guid s_orderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid s_refundId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const decimal Amount = 49.99m;
    private const string CustomerId = "CUST-001";
    private const string Reason = "Item not as described";

    private static RequestQuickRefundRequest CreateRequest()
        => new()
        {
            OrderId = s_orderId,
            Amount = Amount,
            CustomerId = CustomerId,
            Reason = Reason
        };

    private static ProcessRefundResponse CreateSuccessResponse()
        => new()
        {
            RefundId = s_refundId,
            OrderId = s_orderId,
            Amount = Amount,
            Success = true,
            ProcessedAt = DateTimeOffset.UtcNow
        };

    private static ProcessRefundResponse CreateFailureResponse()
        => new()
        {
            RefundId = Guid.Empty,
            OrderId = s_orderId,
            Amount = 0,
            Success = false,
            FailureReason = "Insufficient funds",
            ProcessedAt = DateTimeOffset.UtcNow
        };

    [Fact]
    public async Task Should_TransitionToAwaitingRefund_And_SendProcessRefund_When_RequestReceived()
    {
        var tester = SagaTester.Create(new QuickRefundSaga());

        await tester
            .Plan()
            .On(CreateRequest())
            .ExpectState("AwaitingRefund")
            .ExpectSendMessage<ProcessRefundCommand>(
                (state, cmd) =>
                {
                    Assert.Equal(s_orderId, cmd.OrderId);
                    Assert.Equal(Amount, cmd.Amount);
                    Assert.Equal(Reason, cmd.Reason);
                    Assert.Equal(CustomerId, cmd.CustomerId);
                })
            .RunAll();
    }

    [Fact]
    public async Task Should_SetRefundId_And_Respond_When_RefundSucceeds()
    {
        var tester = SagaTester.Create(new QuickRefundSaga());

        await tester
            .Plan()
            .On(CreateRequest())
            .ExpectState("AwaitingRefund")
            .ThenOn(CreateSuccessResponse())
            .ExpectState("Completed")
            .ExpectReplyMessage<QuickRefundResponse>(
                (state, resp) =>
                {
                    Assert.True(resp.Success);
                    Assert.Equal(s_refundId, resp.RefundId);
                    Assert.Equal(Amount, resp.RefundedAmount);
                    Assert.Equal(s_orderId, resp.OrderId);
                })
            .ExpectCompletion()
            .RunAll();
    }

    [Fact]
    public async Task Should_SetFailureReason_And_Respond_When_RefundFails()
    {
        var tester = SagaTester.Create(new QuickRefundSaga());

        await tester
            .Plan()
            .On(CreateRequest())
            .ExpectState("AwaitingRefund")
            .ThenOn(CreateFailureResponse())
            .ExpectState("Completed")
            .ExpectReplyMessage<QuickRefundResponse>(
                (state, resp) =>
                {
                    Assert.False(resp.Success);
                    Assert.Equal("Insufficient funds", resp.FailureReason);
                    Assert.Null(resp.RefundId);
                    Assert.Equal(s_orderId, resp.OrderId);
                })
            .ExpectCompletion()
            .RunAll();
    }

    [Fact]
    public async Task Should_PopulateStateFromRequest_When_StateFactoryCreates()
    {
        var tester = SagaTester.Create(new QuickRefundSaga());

        await tester.Plan().On(CreateRequest()).RunAll();

        var state = tester.State!;
        Assert.Equal(s_orderId, state.OrderId);
        Assert.Equal(Amount, state.Amount);
        Assert.Equal(CustomerId, state.CustomerId);
        Assert.Equal(Reason, state.Reason);
    }

    [Fact]
    public async Task Should_SetSagaHeaders_InSendOptions_When_SendingCommand()
    {
        var tester = SagaTester.Create(new QuickRefundSaga());

        await tester
            .Plan()
            .On(CreateRequest())
            .ExpectSendMessage<ProcessRefundCommand>()
            .ExpectSendOptions(opts => Assert.NotNull(opts.Headers))
            .RunAll();
    }
}
