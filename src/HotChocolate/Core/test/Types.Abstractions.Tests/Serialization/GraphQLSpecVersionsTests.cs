namespace HotChocolate.Serialization;

public class GraphQLSpecVersionsTests
{
    [Theory]
    [InlineData("  OcToBeR-2021  ", GraphQLSpecVersion.October2021)]
    [InlineData("2021-10", GraphQLSpecVersion.October2021)]
    [InlineData("SePtEmBeR-2025", GraphQLSpecVersion.September2025)]
    [InlineData("2025-09", GraphQLSpecVersion.September2025)]
    public void TryParse_Should_Parse_Supported_Spellings_When_Value_Has_Mixed_Casing(
        string value,
        GraphQLSpecVersion expected)
    {
        // act
        var parsed = GraphQLSpecVersions.TryParse(value, out var actual);

        // assert
        Assert.True(parsed);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("january-2024")]
    public void TryParse_Should_ReturnFalse_When_Value_IsNotSupported(string? value)
    {
        // act
        var parsed = GraphQLSpecVersions.TryParse(value, out _);

        // assert
        Assert.False(parsed);
    }

    [Theory]
    [InlineData(GraphQLSpecVersion.October2021)]
    [InlineData(GraphQLSpecVersion.September2025)]
    public void GetWireName_Should_RoundTrip_When_Version_IsSupported(GraphQLSpecVersion expected)
    {
        // act
        var parsed = GraphQLSpecVersions.TryParse(GraphQLSpecVersions.GetWireName(expected), out var actual);

        // assert
        Assert.True(parsed);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SupportedValues_Should_Return_Canonical_WireNames_In_Enum_Order()
    {
        // act
        var values = GraphQLSpecVersions.SupportedValues;

        // assert
        Assert.Equal(["october-2021", "september-2025"], values);
    }

    [Fact]
    public void SpecVersion_Should_DefaultToNull_When_Options_AreCreated()
    {
        // act
        var options = new SchemaFormatterOptions();

        // assert
        Assert.Null(options.SpecVersion);
    }
}
