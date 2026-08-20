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
}
