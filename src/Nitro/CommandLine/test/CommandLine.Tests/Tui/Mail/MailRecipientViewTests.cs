using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailRecipientViewTests
{
    [Fact]
    public void FindRecipient_Should_ReturnNull_When_ActorIsNotARecipient()
    {
        // arrange
        var message = MailMessageBuilder.Create("m-1", recipients: [MailMessageBuilder.ToRecipient("bob")]);

        // act
        var recipient = MailRecipientView.FindRecipient(message, "alice");

        // assert
        Assert.Null(recipient);
    }

    [Fact]
    public void FindRecipient_Should_MatchCaseInsensitively_When_ActorIsNotNormalized()
    {
        // arrange: the store normalizes recipient names to lowercase, but an
        // actor passed to the mode is not guaranteed to already be
        // normalized.
        var message = MailMessageBuilder.Create("m-1", recipients: [MailMessageBuilder.ToRecipient("alice")]);

        // act
        var recipient = MailRecipientView.FindRecipient(message, "Alice");

        // assert
        Assert.NotNull(recipient);
        Assert.Equal("alice", recipient.Name);
    }

    [Fact]
    public void IsUnread_Should_ReturnTrue_When_ActorHasNotReadTheMessage()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1", recipients: [MailMessageBuilder.ToRecipient("alice", readAt: null)]);

        // act & assert
        Assert.True(MailRecipientView.IsUnread(message, "alice"));
    }

    [Fact]
    public void IsUnread_Should_ReturnFalse_When_ActorHasReadTheMessage()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1", recipients: [MailMessageBuilder.ToRecipient("alice", readAt: DateTimeOffset.UnixEpoch)]);

        // act & assert
        Assert.False(MailRecipientView.IsUnread(message, "alice"));
    }

    [Fact]
    public void IsUnread_Should_ReturnFalse_When_ActorIsNotARecipient()
    {
        // arrange
        var message = MailMessageBuilder.Create("m-1", recipients: [MailMessageBuilder.ToRecipient("bob")]);

        // act & assert
        Assert.False(MailRecipientView.IsUnread(message, "alice"));
    }

    [Fact]
    public void IsArchived_Should_ReturnTrue_When_ActorHasArchivedTheMessage()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1", recipients: [MailMessageBuilder.ToRecipient("alice", archivedAt: DateTimeOffset.UnixEpoch)]);

        // act & assert
        Assert.True(MailRecipientView.IsArchived(message, "alice"));
    }

    [Fact]
    public void IsArchived_Should_ReturnFalse_When_ActorHasNotArchivedTheMessage()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1", recipients: [MailMessageBuilder.ToRecipient("alice", archivedAt: null)]);

        // act & assert
        Assert.False(MailRecipientView.IsArchived(message, "alice"));
    }

    [Fact]
    public void IsFromActor_Should_ReturnTrue_When_ActorIsTheSender()
    {
        // arrange
        var message = MailMessageBuilder.Create("m-1", sender: "alice");

        // act & assert
        Assert.True(MailRecipientView.IsFromActor(message, "alice"));
    }

    [Fact]
    public void IsFromActor_Should_MatchCaseInsensitively_When_ActorIsNotNormalized()
    {
        // arrange
        var message = MailMessageBuilder.Create("m-1", sender: "alice");

        // act & assert
        Assert.True(MailRecipientView.IsFromActor(message, "Alice"));
    }

    [Fact]
    public void IsFromActor_Should_ReturnFalse_When_ActorIsNotTheSender()
    {
        // arrange
        var message = MailMessageBuilder.Create("m-1", sender: "bob");

        // act & assert
        Assert.False(MailRecipientView.IsFromActor(message, "alice"));
    }

    [Fact]
    public void GetPeers_Should_ReturnSender_When_ActorReceivedTheMessage()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1", sender: "bob", recipients: [MailMessageBuilder.ToRecipient("alice")]);

        // act
        var peers = MailRecipientView.GetPeers(message, "alice");

        // assert
        Assert.Equal(["bob"], peers);
    }

    [Fact]
    public void GetPeers_Should_ReturnRecipients_When_ActorSentTheMessage()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "alice",
            recipients:
            [
                MailMessageBuilder.ToRecipient("bob", ordinal: 0),
                MailMessageBuilder.ToRecipient("carol", ordinal: 1)
            ]);

        // act
        var peers = MailRecipientView.GetPeers(message, "alice");

        // assert
        Assert.Equal(["bob", "carol"], peers);
    }

    [Fact]
    public void GetRelationshipGlyph_Should_ReturnFromActorGlyph_When_ActorIsTheSender()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1", sender: "alice", recipients: [MailMessageBuilder.ToRecipient("bob")]);

        // act & assert
        Assert.Equal(MailRecipientView.FromActorGlyph, MailRecipientView.GetRelationshipGlyph(message, "alice"));
    }

    [Fact]
    public void GetRelationshipGlyph_Should_ReturnDirectGlyph_When_ActorIsSoleRecipient()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1", sender: "bob", recipients: [MailMessageBuilder.ToRecipient("alice")]);

        // act & assert
        Assert.Equal(MailRecipientView.DirectGlyph, MailRecipientView.GetRelationshipGlyph(message, "alice"));
    }

    [Fact]
    public void GetRelationshipGlyph_Should_ReturnBroadcastGlyph_When_ActorIsOneOfSeveralRecipients()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1",
            sender: "bob",
            recipients:
            [
                MailMessageBuilder.ToRecipient("alice", ordinal: 0),
                MailMessageBuilder.ToRecipient("carol", ordinal: 1)
            ]);

        // act & assert
        Assert.Equal(MailRecipientView.BroadcastGlyph, MailRecipientView.GetRelationshipGlyph(message, "alice"));
    }

    [Fact]
    public void GetRelationshipGlyph_Should_ReturnBlankGlyph_When_ActorIsNeitherSenderNorRecipient()
    {
        // arrange
        var message = MailMessageBuilder.Create(
            "m-1", sender: "bob", recipients: [MailMessageBuilder.ToRecipient("carol")]);

        // act & assert
        Assert.Equal(MailRecipientView.BlankGlyph, MailRecipientView.GetRelationshipGlyph(message, "alice"));
    }
}
