using Xunit;

namespace StrawberryShake.Tools.Configuration;

public class FileContentsTests
{
    [Fact]
    public void Ensure_Extensions_Are_Correct()
    {
        FileContents.SchemaExtensionFileContent.MatchSnapshot();
    }
}
