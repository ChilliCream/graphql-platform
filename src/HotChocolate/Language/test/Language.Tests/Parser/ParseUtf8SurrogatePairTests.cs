using System.Text;
using Newtonsoft.Json;
using Xunit;

namespace HotChocolate.Language;

public class ParseUtf8SurrogatePairTests
{
    [Fact]
    public void Handle_UTF8_Surrogate_Pairs_Correctly()
    {
        var emojiBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes("😀");
        var reader = new Utf8GraphQLReader(emojiBytes);
        Assert.Equal("😀", System.Text.Json.JsonSerializer.Deserialize<string>(emojiBytes));
        Assert.Equal("😀", JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(emojiBytes)));
        Assert.True(reader.Read());
        Assert.Equal("😀", reader.GetString());
    }
}
