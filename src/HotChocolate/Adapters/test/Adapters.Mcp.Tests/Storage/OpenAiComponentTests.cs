namespace HotChocolate.Adapters.Mcp.Storage;

public sealed class OpenAiComponentTests
{
    [Fact]
    public void OpenAiComponent_Construct_SetsCorrectOutputTemplateUri()
    {
        // arrange
        const string name = "Test Component \\o/"; // name with spaces and special characters
        const string htmlTemplateText = "...";

        // act
        var component = new OpenAiComponent(name, htmlTemplateText);

        // assert
        Assert.Equal(
            "ui://components/test-component-o-AB5DF625BC76DBD4E163BED2DD888DF828F90159BB93556525C31821B6541D46.html",
            component.OutputTemplate);
    }
}
