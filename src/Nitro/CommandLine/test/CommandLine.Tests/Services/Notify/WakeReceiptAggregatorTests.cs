using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises the approved status lattice <see cref="WakeReceiptAggregator"/>
/// derives from a batch's connection statuses: successful terminal states,
/// unresolved work, the no-live-connection case, and mixed terminal results.
/// </summary>
public sealed class WakeReceiptAggregatorTests
{
    [Fact]
    public void Aggregate_Should_ReturnSkipped_When_NoTargetsExist()
        => Assert.Equal(MailWakeTargetStatus.Skipped, WakeReceiptAggregator.Aggregate([]));

    [Fact]
    public void Aggregate_Should_ReturnFailed_When_EveryTargetFailed()
        => Assert.Equal(
            MailWakeTargetStatus.Failed,
            WakeReceiptAggregator.Aggregate([MailWakeTargetStatus.Failed, MailWakeTargetStatus.Failed]));

    [Fact]
    public void Aggregate_Should_ReturnFailed_When_FailedAndDeliveredRecipientsExist()
        => Assert.Equal(
            MailWakeTargetStatus.Failed,
            WakeReceiptAggregator.Aggregate([MailWakeTargetStatus.Failed, MailWakeTargetStatus.Delivered]));

    [Fact]
    public void Aggregate_Should_ReturnPending_When_FailedAndPendingSiblingsExist()
        => Assert.Equal(
            MailWakeTargetStatus.Pending,
            WakeReceiptAggregator.Aggregate([MailWakeTargetStatus.Failed, MailWakeTargetStatus.Pending]));

    [Fact]
    public void Aggregate_Should_ReturnPending_When_NoFailuresButATargetIsStillPending()
        => Assert.Equal(
            MailWakeTargetStatus.Pending,
            WakeReceiptAggregator.Aggregate([MailWakeTargetStatus.Delivered, MailWakeTargetStatus.Pending]));

    [Fact]
    public void Aggregate_Should_ReturnDelivered_When_EveryTargetDelivered()
        => Assert.Equal(
            MailWakeTargetStatus.Delivered,
            WakeReceiptAggregator.Aggregate([MailWakeTargetStatus.Delivered, MailWakeTargetStatus.Delivered]));

    [Fact]
    public void Aggregate_Should_ReturnSatisfied_When_EveryTargetSatisfied()
        => Assert.Equal(
            MailWakeTargetStatus.Satisfied,
            WakeReceiptAggregator.Aggregate([MailWakeTargetStatus.Satisfied, MailWakeTargetStatus.Satisfied]));

    [Fact]
    public void Aggregate_Should_ReturnDelivered_When_SuccessfulStatusesDiffer()
        => Assert.Equal(
            MailWakeTargetStatus.Delivered,
            WakeReceiptAggregator.Aggregate(
                [MailWakeTargetStatus.Satisfied, MailWakeTargetStatus.Delivered]));

    [Theory]
    [InlineData(MailWakeTargetStatus.Delivered, true)]
    [InlineData(MailWakeTargetStatus.Satisfied, true)]
    [InlineData(MailWakeTargetStatus.Delegated, true)]
    [InlineData(MailWakeTargetStatus.Skipped, true)]
    [InlineData(MailWakeTargetStatus.Pending, false)]
    [InlineData(MailWakeTargetStatus.Failed, false)]
    public void IsSuccessful_Should_ClassifyEveryStatus(string status, bool expected)
        => Assert.Equal(expected, WakeReceiptAggregator.IsSuccessful(status));
}
