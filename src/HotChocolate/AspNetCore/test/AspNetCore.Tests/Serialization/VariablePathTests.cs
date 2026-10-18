using System.Globalization;
using HotChocolate.AspNetCore.Parsers;

namespace HotChocolate.AspNetCore.Serialization;

public class VariablePathTests
{
    [Fact]
    public void PathHasNoFields()
    {
        // arrange
        const string path = "variables";

        // act
        void Parse() => VariablePath.Parse(path);

        // assert
        Assert.Equal(
            ErrorCodes.Server.MultiPartInvalidPath,
            Assert.Throws<GraphQLRequestException>(Parse).Errors.Single().Code);
    }

    [Fact]
    public void InvalidRoot()
    {
        // arrange
        const string path = "foo.bar";

        // act
        void Parse() => VariablePath.Parse(path);

        // assert
        Assert.Equal(
            ErrorCodes.Server.MultiPartPathMustStartWithVariable,
            Assert.Throws<GraphQLRequestException>(Parse).Errors.Single().Code);
    }

    [Fact]
    public void PathMustStartWithKey()
    {
        // arrange
        const string path = "variables.1.foo.bar";

        // act
        void Parse() => VariablePath.Parse(path);

        // assert
        Assert.Throws<InvalidOperationException>(Parse);
    }

    [Fact]
    public void ValidPath()
    {
        // arrange
        const string s = "variables.foo.bar.1.baz";

        // act
        var path = VariablePath.Parse(s);

        // assert
        Assert.Equal("foo", path.Key.Value);
        Assert.Equal("bar", Assert.IsType<KeyPathSegment>(path.Key.Next).Value);
        Assert.Equal(1, Assert.IsType<IndexPathSegment>(path.Key.Next.Next).Value);
        Assert.Equal("baz", Assert.IsType<KeyPathSegment>(path.Key.Next.Next.Next).Value);
    }

    [Theory]
    [InlineData("variables.upload", new object[] { "upload" })]
    [InlineData(".variables..input.0.0.file.", new object[] { "input", 0, 0, "file" })]
    [InlineData("variables.foo.variables.01.bar", new object[] { "foo", 1, "bar" })]
    [InlineData("variables. file ", new object[] { " file " })]
    [InlineData("variables.files. +1 ", new object[] { "files", 1 })]
    [InlineData("variables.files.2147483648", new object[] { "files", "2147483648" })]
    [InlineData("variables.2147483648", new object[] { "2147483648" })]
    public void Parse_Should_PreserveSegments_When_PathsContainEmptyOrSpecialTokens(string input, object[] expected)
    {
        // arrange
        // act
        var path = VariablePath.Parse(input);

        // assert
        Assert.Equal(expected, GetValues(path));
    }

    [Theory]
    [InlineData("", "Invalid variable path `` in `map`.", "HC0041")]
    [InlineData("...", "Invalid variable path `...` in `map`.", "HC0041")]
    [InlineData("variables", "Invalid variable path `variables` in `map`.", "HC0041")]
    [InlineData("..wrong..", "Invalid variable path `..wrong..` in `map`.", "HC0041")]
    [InlineData("wrong.file", "The variable path must start with `variables`.", "HC0042")]
    [InlineData("Variables.file", "The variable path must start with `variables`.", "HC0042")]
    public void Parse_Should_PreserveValidationErrors_When_PathIsInvalid(
        string input,
        string message,
        string code)
    {
        // arrange
        Action parse = () => VariablePath.Parse(input);

        // act
        var exception = Assert.Throws<GraphQLRequestException>(parse);

        // assert
        Assert.Equal(message, exception.Message);
        var error = Assert.Single(exception.Errors);
        Assert.Equal(message, error.Message);
        Assert.Equal(code, error.Code);
    }

    [Fact]
    public void Parse_Should_ThrowNullReferenceException_When_PathIsNull()
    {
        // arrange
        Action parse = () => VariablePath.Parse(null!);

        // act
        // assert
        Assert.Throws<NullReferenceException>(parse);
    }

    [Theory]
    [InlineData("variables.1.foo")]
    [InlineData("variables.variables")]
    public void Parse_Should_RejectPath_When_FirstSegmentIsNotAKey(string input)
    {
        // arrange
        Action parse = () => VariablePath.Parse(input);

        // act
        var exception = Assert.Throws<InvalidOperationException>(parse);

        // assert
        Assert.Equal("The first path segment must be a key.", exception.Message);
    }

    [Fact]
    public void Parse_Should_UseCultureInReverseOrder_When_NumericFormatsChange()
    {
        // arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var culture = new OrderedNumberCulture();

        try
        {
            CultureInfo.CurrentCulture = culture;

            // act
            var path = VariablePath.Parse(".variables..head.variables.!2..~1.");

            // assert
            Assert.Equal(3, culture.FormatRequests);
            Assert.Equal(new object[] { "head", -2, -1 }, GetValues(path));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("wrong.head.~1", typeof(GraphQLRequestException))]
    [InlineData("variables", typeof(GraphQLRequestException))]
    [InlineData("variables.variables", typeof(InvalidOperationException))]
    public void Parse_Should_RejectPathBeforeReadingCulture_When_PathIsInvalid(string input, Type exceptionType)
    {
        // arrange
        var originalCulture = CultureInfo.CurrentCulture;
        var culture = new OrderedNumberCulture();

        try
        {
            CultureInfo.CurrentCulture = culture;

            // act
            var exception = Record.Exception(() => VariablePath.Parse(input));

            // assert
            Assert.IsType(exceptionType, exception);
            Assert.Equal(0, culture.FormatRequests);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private static IEnumerable<object> GetValues(VariablePath path)
        => path.Select(segment => segment is KeyPathSegment key
            ? (object)key.Value
            : ((IndexPathSegment)segment).Value);

    private sealed class OrderedNumberCulture : CultureInfo
    {
        private readonly NumberFormatInfo _first = new() { NegativeSign = "~" };
        private readonly NumberFormatInfo _later = new() { NegativeSign = "!" };

        public OrderedNumberCulture() : base("en-US")
        {
        }

        public int FormatRequests { get; private set; }

        public override NumberFormatInfo NumberFormat
        {
            get
            {
                FormatRequests++;
                return FormatRequests == 1 ? _first : _later;
            }
            set => base.NumberFormat = value;
        }
    }
}
