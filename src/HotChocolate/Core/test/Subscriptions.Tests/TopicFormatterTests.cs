namespace HotChocolate.Subscriptions;

public class TopicFormatterTests
{
    [Fact]
    public void Format_Topic_Without_Prefix()
        => new TopicFormatter(null)
            .Format("abc")
            .MatchSnapshot();

    [Fact]
    public void Format_Topic_With_Prefix()
        => new TopicFormatter("abc")
            .Format("abc")
            .MatchSnapshot();
}
