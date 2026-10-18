using System.Globalization;
using CookieCrumble;
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

    [Fact]
    public void Parse_Should_PreserveSegments_When_PathsContainEmptyOrSpecialTokens()
    {
        // arrange
        string[] inputs =
        [
            "variables.upload",
            ".variables..input.0.0.file.",
            "variables.foo.variables.01.bar",
            "variables. file ",
            "variables.files. +1 ",
            "variables.files.2147483648",
            "variables.2147483648"
        ];

        // act
        var results = inputs
            .Select(input => new { Input = input, Segments = Describe(VariablePath.Parse(input)) })
            .ToArray();

        // assert
        Snapshot.Create()
            .Add(results, "Complete Linked Segments")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Parse_Should_PreserveValidationErrors_When_PathIsInvalid()
    {
        // arrange
        (string? Input, Type ExceptionType)[] cases =
        [
            (null, typeof(NullReferenceException)),
            (string.Empty, typeof(GraphQLRequestException)),
            ("...", typeof(GraphQLRequestException)),
            ("variables", typeof(GraphQLRequestException)),
            ("..wrong..", typeof(GraphQLRequestException)),
            ("wrong.file", typeof(GraphQLRequestException)),
            ("Variables.file", typeof(GraphQLRequestException)),
            ("variables.1.foo", typeof(InvalidOperationException)),
            ("variables.variables", typeof(InvalidOperationException))
        ];
        var results = new List<object>();

        // act
        foreach (var item in cases)
        {
            var exception = Record.Exception(() =>
            {
                _ = VariablePath.Parse(item.Input!);
            });
            Assert.Equal(item.ExceptionType, exception?.GetType());
            results.Add(new
            {
                item.Input,
                Exception = exception!.GetType().Name,
                exception.Message,
                Errors = exception is GraphQLRequestException requestException
                    ? requestException.Errors.Select(error => new { error.Message, error.Code }).ToArray()
                    : []
            });
        }

        // assert
        Snapshot.Create()
            .Add(results, "Validation Order And Errors")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Parse_Should_UseCultureInReverseOrder_When_NumericFormatsChange()
    {
        // arrange
        var originalCulture = CultureInfo.CurrentCulture;
        (string Input, Type? ExceptionType, int FormatRequests)[] cases =
        [
            (".variables..head.variables.!2..~1.", null, 3),
            ("wrong.head.~1", typeof(GraphQLRequestException), 0),
            ("variables", typeof(GraphQLRequestException), 0),
            ("variables.variables", typeof(InvalidOperationException), 0)
        ];
        var results = new List<object>();

        // act
        try
        {
            foreach (var item in cases)
            {
                var culture = new OrderedNumberCulture();
                CultureInfo.CurrentCulture = culture;
                VariablePath? path = null;
                var exception = Record.Exception(() =>
                {
                    path = VariablePath.Parse(item.Input);
                });
                Assert.Equal(item.ExceptionType, exception?.GetType());
                Assert.Equal(item.FormatRequests, culture.FormatRequests);
                results.Add(new
                {
                    item.Input,
                    Segments = path is null ? [] : Describe(path),
                    culture.FormatRequests,
                    Exception = exception?.GetType().Name
                });
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        // assert
        Snapshot.Create()
            .Add(results, "Reverse Culture Access And Linked Segments")
            .MatchMarkdownSnapshot();
    }

    private static object[] Describe(VariablePath path)
        => path.Select(segment => new
        {
            Type = segment.GetType().Name,
            Value = segment is KeyPathSegment key ? (object)key.Value : ((IndexPathSegment)segment).Value
        }).ToArray();

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
