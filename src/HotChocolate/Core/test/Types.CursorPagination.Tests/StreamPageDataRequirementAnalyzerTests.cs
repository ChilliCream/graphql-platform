using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Types.Pagination;

public class StreamPageDataRequirementAnalyzerTests
{
    [Theory]
    [InlineData("@stream", true)]
    [InlineData("@stream(initialCount: 1)", true)]
    [InlineData("@stream(if: true)", true)]
    [InlineData("@stream(if: false)", false)]
    public void IsStreamEnabled_Should_EvaluateLiteral_When_IfIsNotAVariable(string directive, bool expected)
    {
        // arrange
        var node = Utf8GraphQLParser.Syntax.ParseField($"nodes {directive}").Directives[0];

        // act
        var enabled = StreamPageDataRequirementAnalyzer.IsStreamEnabled(node, new TestVariables());

        // assert
        Assert.Equal(expected, enabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsStreamEnabled_Should_ReturnVariableValue_When_VariableIsProvided(bool value)
    {
        // arrange
        var node = Utf8GraphQLParser.Syntax.ParseField("nodes @stream(if: $s)").Directives[0];
        var variables = new TestVariables(new BooleanValueNode(value));

        // act
        var enabled = StreamPageDataRequirementAnalyzer.IsStreamEnabled(node, variables);

        // assert
        Assert.Equal(value, enabled);
    }

    [Fact]
    public void IsStreamEnabled_Should_ReturnTrue_When_VariableIsNotProvided()
    {
        // arrange
        var node = Utf8GraphQLParser.Syntax.ParseField("nodes @stream(if: $s)").Directives[0];

        // act
        var enabled = StreamPageDataRequirementAnalyzer.IsStreamEnabled(node, new TestVariables());

        // assert
        Assert.True(enabled);
    }

    [Fact]
    public void IsStreamEnabled_Should_Throw_When_VariableIsNull()
    {
        // arrange
        var node = Utf8GraphQLParser.Syntax.ParseField("nodes @stream(if: $s)").Directives[0];
        var variables = new TestVariables(NullValueNode.Default);

        // act
        var exception = Assert.Throws<GraphQLException>(
            () => StreamPageDataRequirementAnalyzer.IsStreamEnabled(node, variables));

        // assert
        Assert.Equal(
            "The variable `s` is null, but the `if` argument of the `@stream` directive must not be null.",
            exception.Errors[0].Message);
    }

    [Fact]
    public void IsStreamEnabled_Should_Throw_When_IfIsNotABoolean()
    {
        // arrange
        var node = Utf8GraphQLParser.Syntax.ParseField("nodes @stream(if: \"yes\")").Directives[0];

        // act
        var exception = Assert.Throws<GraphQLException>(
            () => StreamPageDataRequirementAnalyzer.IsStreamEnabled(node, new TestVariables()));

        // assert
        Assert.Equal(
            "The `if` argument of the `@stream` directive must be a Boolean.",
            exception.Errors[0].Message);
    }

    private sealed class TestVariables(IValueNode? value = null) : IVariableValueCollection
    {
        public bool IsEmpty => value is null;

        public T GetValue<T>(string name) where T : IValueNode
            => TryGetValue<T>(name, out var result) ? result : throw new KeyNotFoundException(name);

        public bool TryGetValue<T>(string name, [NotNullWhen(true)] out T? result) where T : IValueNode
        {
            if (value is T casted)
            {
                result = casted;
                return true;
            }

            result = default;
            return false;
        }

        public IEnumerator<VariableValue> GetEnumerator()
            => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
