using System.Text;

namespace HotChocolate.Types;

public class EnumValueCollectionTests
{
    [Fact]
    public void TryGetValue_Should_ReturnValue_When_SmallEnumContainsUtf8Name()
    {
        // arrange
        var values = CreateValues("RED", "GREEN", "BLUE");

        // act
        var found = values.TryGetValue("GREEN"u8, out var value);

        // assert
        Assert.True(found);
        Assert.Equal("GREEN", value!.Name);
    }

    [Fact]
    public void TryGetValue_Should_ReturnValue_When_LargeEnumContainsUtf8Name()
    {
        // arrange
        var values = CreateValues(CreateNames(20));

        // act
        var found = values.TryGetValue("V17"u8, out var value);

        // assert
        Assert.True(found);
        Assert.Equal("V17", value!.Name);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(20)]
    public void TryGetValue_Should_ReturnFalse_When_Utf8NameIsUnknown(int count)
    {
        // arrange
        var values = CreateValues(CreateNames(count));

        // act
        var found = values.TryGetValue("UNKNOWN"u8, out var value);

        // assert
        Assert.False(found);
        Assert.Null(value);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(20)]
    public void TryGetValue_Should_ReturnFalse_When_Utf8NameContainsNonAsciiBytes(int count)
    {
        // arrange
        var values = CreateValues(CreateNames(count));

        // act
        var found = values.TryGetValue("VÉ"u8, out var value);

        // assert
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_Should_ReturnValue_When_Utf8NameExceedsStackallocThreshold()
    {
        // arrange
        var longName = "V" + new string('X', 199);
        var values = CreateValues([.. CreateNames(20), longName]);

        // act
        var found = values.TryGetValue(Encoding.UTF8.GetBytes(longName), out var value);

        // assert
        Assert.True(found);
        Assert.Equal(longName, value!.Name);
    }

    [Fact]
    public void TryGetValue_Should_IgnoreCase_When_NameComparerIgnoresCase()
    {
        // arrange
        var values = CreateValues(StringComparer.OrdinalIgnoreCase, "RED", "GREEN", "BLUE");

        // act
        var found = values.TryGetValue("green"u8, out var value);

        // assert
        Assert.True(found);
        Assert.Equal("GREEN", value!.Name);
    }

    [Fact]
    public void TryGetValue_Should_UseComparer_When_NameComparerIsCustom()
    {
        // arrange
        var values = CreateValues(new TrimmingComparer(), "RED", "GREEN", "BLUE");

        // act
        var found = values.TryGetValue(" GREEN "u8, out var value);

        // assert
        Assert.True(found);
        Assert.Equal("GREEN", value!.Name);
    }

    private static string[] CreateNames(int count)
        => [.. Enumerable.Range(0, count).Select(i => "V" + i)];

    private static EnumValueCollection CreateValues(params string[] names)
        => CreateValues(StringComparer.Ordinal, names);

    private static EnumValueCollection CreateValues(IEqualityComparer<string> comparer, params string[] names)
    {
        var schema = SchemaBuilder.New()
            .AddEnumType(d =>
            {
                d.Name("Test").NameComparer(comparer);

                foreach (var name in names)
                {
                    d.Value(name).Name(name);
                }
            })
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        return schema.Types.GetType<EnumType>("Test").Values;
    }

    private sealed class TrimmingComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y)
            => string.Equals(x?.Trim(), y?.Trim(), StringComparison.Ordinal);

        public int GetHashCode(string obj)
            => obj.Trim().GetHashCode();
    }
}
