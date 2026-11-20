using Mocha.Inbox;

namespace Mocha.Tests.Inbox;

public class InboxConsumerMiddlewaresTests
{
    [Fact]
    public void Inbox_Should_UseInboxKey_When_Accessed()
    {
        // act
        var key = InboxConsumerMiddlewares.Inbox.Key;

        // assert
        Assert.Equal("Inbox", key);
    }
}
