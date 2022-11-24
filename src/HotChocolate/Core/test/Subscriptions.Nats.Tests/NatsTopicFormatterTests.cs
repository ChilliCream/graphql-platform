using CookieCrumble;

namespace HotChocolate.Subscriptions.Nats;

public class NatsTopicFormatterTests
{
    [Fact]
    public void Format_Topic_Without_Prefix()
        => new NatsTopicFormatter(null)
            .Format("abc")
            .MatchSnapshot();

    [Fact]
    public void Format_Topic_With_Prefix()
        => new NatsTopicFormatter("abc")
            .Format("abc")
            .MatchSnapshot();
}
