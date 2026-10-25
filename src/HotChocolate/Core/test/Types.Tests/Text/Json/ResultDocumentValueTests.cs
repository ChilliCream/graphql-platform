using System.Text;

namespace HotChocolate.Text.Json;

public class ResultDocumentValueTests
{
    [Fact]
    public void SetStringValue_Should_RoundTripBytes_When_WritingSingleMultiMegabyteValue()
    {
        // arrange
        var expected = new byte[5 * 1024 * 1024];
        var operation = CommonTestExtensions.CreateOperation();
        using var resultDocument = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");

        for (var i = 0; i < expected.Length; i++)
        {
            expected[i] = (byte)('a' + (i % 26));
        }

        // act
        resultValue.SetStringValue(expected);
        var actual = Encoding.UTF8.GetBytes(resultValue.GetString()!);

        // assert
        Assert.Equal(expected, actual);
    }
}
