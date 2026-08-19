using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class SubjectMatcherTests
{
    [Theory]
    [InlineData("order-service.>", "order-service.order-created", true)]
    [InlineData("order-service.>", "order-service.orders.created", true)]
    [InlineData("order-service.>", "order-service", false)]
    [InlineData("order-service.*", "order-service.order-created", true)]
    [InlineData("order-service.*", "order-service.orders.created", false)]
    [InlineData("order-service.order-created", "order-service.order-created", true)]
    [InlineData("order-service.order-created", "order-service.order-updated", false)]
    [InlineData("order-service.order-created", "order-service.order-created.v2", false)]
    [InlineData("*.order-created", "order-service.order-created", true)]
    [InlineData(">", "anything.at.all", true)]
    public void Matches_Should_FollowNatsWildcardRules_When_GivenAFilterAndSubject(
        string filter,
        string subject,
        bool expected)
    {
        // act and assert
        Assert.Equal(expected, SubjectMatcher.Matches(filter, subject));
    }

    [Theory]
    [InlineData("order-service.>", true)]
    [InlineData("order-service.*", true)]
    [InlineData("*", true)]
    [InlineData(">", true)]
    [InlineData("order-service.order-created", false)]
    [InlineData("order-service.order-created.v2", false)]
    // Only a whole token counts, so these are ordinary subjects despite containing the characters.
    [InlineData("order-service.v>2", false)]
    [InlineData("order-service.a*b", false)]
    public void IsWildcard_Should_MatchWholeTokensOnly_When_GivenASubject(string subject, bool expected)
    {
        // act and assert
        Assert.Equal(expected, SubjectMatcher.IsWildcard(subject));
    }

    [Fact]
    public void Collapse_Should_DropASubject_When_AnotherAlreadyCoversIt()
    {
        // arrange
        // The transport derives a subject per message type and the caller may filter a wildcard over
        // the same family, and JetStream rejects a set holding both.
        string[] subjects = ["contracts.orders.cancel-order", "contracts.orders.>"];

        // act
        var collapsed = SubjectMatcher.Collapse(subjects);

        // assert
        Assert.Equal(["contracts.orders.>"], collapsed);
    }

    [Fact]
    public void Collapse_Should_DropASubject_When_TheCoveringOneCameFirst()
    {
        // arrange
        // Order must not matter: which of the two is discovered first is incidental.
        string[] subjects = ["contracts.orders.>", "contracts.orders.cancel-order"];

        // act
        var collapsed = SubjectMatcher.Collapse(subjects);

        // assert
        Assert.Equal(["contracts.orders.>"], collapsed);
    }

    [Fact]
    public void Collapse_Should_KeepBoth_When_TheSubjectsAreDisjoint()
    {
        // arrange
        string[] subjects = ["contracts.orders.>", "contracts.shipments.>"];

        // act
        var collapsed = SubjectMatcher.Collapse(subjects);

        // assert
        Assert.Equal(["contracts.orders.>", "contracts.shipments.>"], collapsed);
    }

    [Fact]
    public void Collapse_Should_RemoveRepeats_When_GivenTheSameSubjectTwice()
    {
        // arrange
        string[] subjects = ["contracts.orders.cancel-order", "contracts.orders.cancel-order"];

        // act
        var collapsed = SubjectMatcher.Collapse(subjects);

        // assert
        Assert.Equal(["contracts.orders.cancel-order"], collapsed);
    }

    [Fact]
    public void Collapse_Should_KeepBoth_When_TwoWildcardsOverlapWithoutCovering()
    {
        // arrange
        // Neither covers the other, though both capture contracts.orders.cancel-order. Only a covering
        // subject is collapsed; the server rejects this combination naming the overlap.
        string[] subjects = ["contracts.*.cancel-order", "contracts.orders.>"];

        // act
        var collapsed = SubjectMatcher.Collapse(subjects);

        // assert
        Assert.Equal(["contracts.*.cancel-order", "contracts.orders.>"], collapsed);
    }

    [Fact]
    public void Collapse_Should_ReturnEmpty_When_GivenNoSubjects()
    {
        // act and assert
        Assert.Empty(SubjectMatcher.Collapse([]));
    }
}
