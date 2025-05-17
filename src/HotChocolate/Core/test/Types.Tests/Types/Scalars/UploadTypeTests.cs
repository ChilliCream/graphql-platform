using HotChocolate.Language;
using Moq;

namespace HotChocolate.Types;

public class UploadTypeTests
{
    [Fact]
    public void FileValueNode_Format()
    {
        // arrange
        var file = new Mock<IFile>();
        file.Setup(t => t.Name).Returns("abc.json");

        var objectValue = new ObjectValueNode(
            new ObjectFieldNode(
                "abc",
                new FileValueNode(file.Object)));

        // act & assert
        objectValue.MatchInlineSnapshot(
            """
            { abc: "abc.json" }
            """);
    }
}
