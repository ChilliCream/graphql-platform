using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Language;
using Moq;

namespace HotChocolate.Execution.Processing;

public class VisibilityTests
{
    [Fact]
    public void Equals_Skip_With_Variable_True()
    {
        // arrange
        var fieldA = Utf8GraphQLParser.Syntax.ParseField("fieldA @skip(if: $a)");
        var fieldB = Utf8GraphQLParser.Syntax.ParseField("fieldB @skip(if: $a)");

        // act
        var hasIncludeConditionA = IncludeCondition.TryCreate(fieldA, out var includeConditionA);
        var hasIncludeConditionB = IncludeCondition.TryCreate(fieldB, out var includeConditionB);

        // assert
        Assert.True(hasIncludeConditionA);
        Assert.True(hasIncludeConditionB);
        Assert.True(includeConditionA.Equals(includeConditionB));
    }

    [Fact]
    public void TryExtract_False()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        var field = Utf8GraphQLParser.Syntax.ParseField("field @test(test: true)");

        // act
        var hasIncludeCondition = IncludeCondition.TryCreate(field, out var includeCondition);

        // assert
        Assert.False(hasIncludeCondition);
    }

    [Fact]
    public void TryExtract_False_2()
    {
        // arrange
        var variables = new Mock<IVariableValueCollection>();
        var field = Utf8GraphQLParser.Syntax.ParseField("field");

        // act
        var hasIncludeCondition = IncludeCondition.TryCreate(field, out var includeCondition);

        // assert
        Assert.False(hasIncludeCondition);
    }

    [Fact]
    public void IsVisible_Skip_Variables_True()
    {
        // arrange
        var variables = new MockVariables(BooleanValueNode.False);
        var field = Utf8GraphQLParser.Syntax.ParseField("field @skip(if: $a)");

        // act
        var hasIncludeCondition = IncludeCondition.TryCreate(field, out var includeCondition);

        // assert
        Assert.True(hasIncludeCondition);
        Assert.True(includeCondition.IsIncluded(variables));
    }

    [Fact]
    public void IsVisible_Include_Variables_True()
    {
        // arrange
        var variables = new MockVariables(BooleanValueNode.True);
        var field = Utf8GraphQLParser.Syntax.ParseField("field @include(if: $a)");

        // act
        var hasIncludeCondition = IncludeCondition.TryCreate(field, out var includeCondition);

        // assert
        Assert.True(hasIncludeCondition);
        Assert.True(includeCondition.IsIncluded(variables));
    }

    private class MockVariables(BooleanValueNode value) : IVariableValueCollection
    {
        public BooleanValueNode Value { get; } = value;

        public bool IsEmpty { get; }

        public T GetValue<T>(string name) where T : IValueNode
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue<T>(string name, [NotNullWhen(true)] out T? value) where T : IValueNode
        {
            value = (T)(object)Value;
            return true;
        }

        public IEnumerator<Execution.VariableValue> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
