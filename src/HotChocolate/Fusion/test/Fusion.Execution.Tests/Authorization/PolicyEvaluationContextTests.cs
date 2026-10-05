using System.Collections.Immutable;
using static HotChocolate.Fusion.Authorization.PolicyTestHelper;

namespace HotChocolate.Fusion.Authorization;

public class PolicyEvaluationContextTests
{
    [Fact]
    public void Verdicts_Should_BeUnanswered_When_NoEntryWasTouched()
    {
        // arrange
        var selections = CreateSelections();
        var policy = AuthenticatedPolicy.Instance;
        var context = CreateContext(
            Anonymous(),
            CreateEntry(selections[1], policy),
            CreateEntry(selections[2], policy));

        // act
        var outcomes = Outcomes(context);

        // assert
        Assert.Equal([PolicyOutcome.Unanswered, PolicyOutcome.Unanswered], outcomes);
    }

    [Fact]
    public void Allow_Should_OnlyAnswerTheGivenEntry_When_CalledForOneEntry()
    {
        // arrange
        var selections = CreateSelections();
        var policy = AuthenticatedPolicy.Instance;
        var context = CreateContext(
            Anonymous(),
            CreateEntry(selections[1], policy),
            CreateEntry(selections[2], policy));

        // act
        context.Allow(context.Entries[1]);

        // assert
        Assert.Equal([PolicyOutcome.Unanswered, PolicyOutcome.Allowed], Outcomes(context));
    }

    [Fact]
    public void Allow_Should_NotOverrideDenial_When_EntryWasDeniedBefore()
    {
        // arrange
        var selections = CreateSelections();
        var context = CreateContext(
            Anonymous(),
            CreateEntry(selections[1], AuthenticatedPolicy.Instance));
        context.Deny(context.Entries[0], "no");

        // act
        context.Allow(context.Entries[0]);

        // assert
        Assert.Equal(
            new PolicyVerdict(PolicyOutcome.Denied, "no", null),
            context.Verdicts[0]);
    }

    [Fact]
    public void Deny_Should_ReplaceAllowance_When_EntryWasAllowedBefore()
    {
        // arrange
        var selections = CreateSelections();
        var context = CreateContext(
            Anonymous(),
            CreateEntry(selections[1], AuthenticatedPolicy.Instance));
        context.Allow(context.Entries[0]);

        // act
        context.Deny(context.Entries[0], "later");

        // assert
        Assert.Equal(
            new PolicyVerdict(PolicyOutcome.Denied, "later", null),
            context.Verdicts[0]);
    }

    [Fact]
    public void Deny_Should_RecordReasonAndAuditData_When_Provided()
    {
        // arrange
        var selections = CreateSelections();
        var context = CreateContext(
            Anonymous(),
            CreateEntry(selections[1], AuthenticatedPolicy.Instance));
        var auditData = ImmutableDictionary<string, string>.Empty.Add("rule", "r1");

        // act
        context.Deny(context.Entries[0], "no", auditData);

        // assert
        var verdict = context.Verdicts[0];
        Assert.Equal(PolicyOutcome.Denied, verdict.Outcome);
        Assert.Equal("no", verdict.Reason);
        Assert.Same(auditData, verdict.AuditData);
    }

    [Fact]
    public void Allow_Should_Throw_When_EntryIsNotPartOfTheContext()
    {
        // arrange
        var selections = CreateSelections();
        var policy = AuthenticatedPolicy.Instance;
        var context = CreateContext(Anonymous(), CreateEntry(selections[1], policy));
        var foreign = CreateEntry(selections[1], policy);

        // act
        void Act() => context.Allow(foreign);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("The policy entry is not part of this evaluation context.", exception.Message);
    }
}
