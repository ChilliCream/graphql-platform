using CookieCrumble;

namespace HotChocolate.Subscriptions;

public class DefaultJsonMessageSerializerTests
{
    [Fact]
    public void SerializeDefaultMessage()
    {
        // arrange
        var serializer = new DefaultJsonMessageSerializer();
        var message = "abc";

        // act
        var serializedMessage = serializer.Serialize(message);

        // assert
        Snapshot
            .Create()
            .Add(serializedMessage)
            .MatchInline("{\"body\":\"abc\",\"kind\":0}");
    }
}
