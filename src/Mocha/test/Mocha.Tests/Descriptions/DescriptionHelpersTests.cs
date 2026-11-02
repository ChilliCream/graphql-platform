namespace Mocha.Tests.Descriptions;

public class DescriptionHelpersTests
{
    [Fact]
    public void GetTypeName_Should_Return_Simple_Name_When_Non_Generic_Type()
    {
        // arrange & act
        var name = DescriptionHelpers.GetTypeName(typeof(string));

        // assert
        Assert.Equal("String", name);
    }

    [Fact]
    public void GetTypeName_Should_Return_Formatted_Name_When_Generic_Type()
    {
        // arrange & act
        var name = DescriptionHelpers.GetTypeName(typeof(List<string>));

        // assert
        Assert.Equal("List<String>", name);
    }

    [Fact]
    public void GetTypeName_Should_Handle_Nested_Generics_When_Complex_Type()
    {
        // arrange & act
        var name = DescriptionHelpers.GetTypeName(typeof(Dictionary<string, List<int>>));

        // assert
        Assert.Equal("Dictionary<String, List<Int32>>", name);
    }

    [Fact]
    public void GetTypeName_Should_Return_Simple_Name_When_Value_Type()
    {
        // arrange & act
        var name = DescriptionHelpers.GetTypeName(typeof(int));

        // assert
        Assert.Equal("Int32", name);
    }
}
