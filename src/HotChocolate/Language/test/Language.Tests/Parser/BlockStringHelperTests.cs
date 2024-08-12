using System.Text;
using Xunit;

namespace HotChocolate.Language;

public class BlockStringHelperTests
{
    [Fact]
    public void TrimLeadingEmptyLines()
    {
        // arrange
        var blockString = "\n\n\n\nblock string uses ";
        var input = Encoding.UTF8.GetBytes(blockString);
        var output = new Span<byte>(new byte[input.Length]);

        // act
        StringHelper.TrimBlockStringToken(input, ref output);

        // assert
        Assert.Equal(
            "block string uses ",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void NoTrimNeeded()
    {
        // arrange
        var blockString = "foo";
        var input = Encoding.UTF8.GetBytes(blockString);
        var output = new Span<byte>(new byte[input.Length]);

        // act
        StringHelper.TrimBlockStringToken(input, ref output);

        // assert
        Assert.Equal(
            blockString,
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void TrimTrailingEmptyLines()
    {
        // arrange
        var blockString = "block string uses \n\n\n\n";
        var input = Encoding.UTF8.GetBytes(blockString);
        var output = new Span<byte>(new byte[input.Length]);

        // act
        StringHelper.TrimBlockStringToken(input, ref output);

        // assert
        Assert.Equal(
            "block string uses ",
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [InlineData(
        "block string uses\n    block string uses",
        "block string uses\nblock string uses")]
    [InlineData(
        "    block string uses\n    block string uses",
        "    block string uses\nblock string uses")]
    [InlineData(
        "    block string uses\n\tblock string uses",
        "    block string uses\nblock string uses")]
    [InlineData(
        "    block string uses\r\n\tblock string uses",
        "    block string uses\nblock string uses")]
    [InlineData(
        "    block string uses\r\tblock string uses",
        "    block string uses\nblock string uses")]
    [InlineData(
        "block string uses\n    block string uses\n    block string uses",
        "block string uses\nblock string uses\nblock string uses")]
    [Theory]
    public void TrimCommonIndent(
        string blockString,
        string trimmedBlockString)
    {
        // arrange
        var input = Encoding.UTF8.GetBytes(blockString);
        var output = new Span<byte>(new byte[input.Length]);

        // act
        StringHelper.TrimBlockStringToken(input, ref output);

        // assert
        Assert.Equal(
            trimmedBlockString,
            Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public void SingleLineSingleChar_Does_Not_Loop()
    {
        // arrange
        var blockString = ".";
        var input = Encoding.UTF8.GetBytes(blockString);
        var output = new Span<byte>(new byte[input.Length]);

        // act
        StringHelper.TrimBlockStringToken(input, ref output);

        // assert
        Assert.Equal(
            ".",
            Encoding.UTF8.GetString(output.ToArray()));
    }
}
