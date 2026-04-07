using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types.Descriptors;
using System.Reflection;

namespace HotChocolate.Types;

public sealed class DurationTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange & act
        var type = new DurationType();

        // assert
        Assert.Equal("Duration", type.Name);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "PT5M")]
    [InlineData(DurationFormat.DotNet, "00:05:00")]
    public void CoerceInputLiteral(DurationFormat format, string literalValue)
    {
        // arrange
        var type = new DurationType(format);
        var literal = new StringValueNode(literalValue);
        var expectedTimeSpan = TimeSpan.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedTimeSpan, runtimeValue);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "P10675199DT2H48M5.4775807S")]
    [InlineData(DurationFormat.DotNet, "10675199.02:48:05.4775807")]
    public void CoerceInputLiteral_MaxValue(DurationFormat format, string literalValue)
    {
        // arrange
        var type = new DurationType(format);
        var literal = new StringValueNode(literalValue);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(TimeSpan.MaxValue, runtimeValue);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "-P10675199DT2H48M5.4775808S")]
    [InlineData(DurationFormat.DotNet, "-10675199.02:48:05.4775808")]
    public void CoerceInputLiteral_MinValue(DurationFormat format, string literalValue)
    {
        // arrange
        var type = new DurationType(format);
        var literal = new StringValueNode(literalValue);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(TimeSpan.MinValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new DurationType();
        var literal = new StringValueNode("bad");

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "PT5M")]
    [InlineData(DurationFormat.DotNet, "00:05:00")]
    public void CoerceInputValue(DurationFormat format, string value)
    {
        // arrange
        var type = new DurationType(format);
        var inputValue = JsonDocument.Parse($"\"{value}\"").RootElement;
        var expectedTimeSpan = TimeSpan.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expectedTimeSpan, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new DurationType();
        var inputValue = JsonDocument.Parse("\"bad\"").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "PT5M")]
    [InlineData(DurationFormat.DotNet, "00:05:00")]
    public void CoerceOutputValue(DurationFormat format, string expectedValue)
    {
        // arrange
        var type = new DurationType(format);
        var runtimeValue = TimeSpan.FromMinutes(5);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        Assert.Equal(expectedValue, resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new DurationType();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue("bad", resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "PT5M")]
    [InlineData(DurationFormat.DotNet, "00:05:00")]
    public void ValueToLiteral(DurationFormat format, string expectedValue)
    {
        // arrange
        var type = new DurationType(format);
        var runtimeValue = TimeSpan.FromMinutes(5);

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(expectedValue, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "P10675199DT2H48M5.4775807S")]
    [InlineData(DurationFormat.DotNet, "10675199.02:48:05.4775807")]
    public void ValueToLiteral_MaxValue(DurationFormat format, string expectedValue)
    {
        // arrange
        var type = new DurationType(format);
        var runtimeValue = TimeSpan.MaxValue;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(expectedValue, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "-P10675199DT2H48M5.4775808S")]
    [InlineData(DurationFormat.DotNet, "-10675199.02:48:05.4775808")]
    public void ValueToLiteral_MinValue(DurationFormat format, string expectedValue)
    {
        // arrange
        var type = new DurationType(format);
        var runtimeValue = TimeSpan.MinValue;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(expectedValue, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "PT5M")]
    [InlineData(DurationFormat.DotNet, "00:05:00")]
    public void ParseLiteral(DurationFormat format, string literalValue)
    {
        // arrange
        var type = new DurationType(format);
        var literal = new StringValueNode(literalValue);
        var expectedTimeSpan = TimeSpan.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedTimeSpan, Assert.IsType<TimeSpan>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new DurationType();

        // act
        void Action() => type.CoerceInputLiteral(new IntValueNode(123));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void Ensure_TypeKind_Is_Scalar()
    {
        // arrange
        var type = new DurationType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    public void ImplementationFirst_AutomaticallyBinds_TimeSpan()
    {
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [InlineData(DurationFormat.Iso8601)]
    [InlineData(DurationFormat.DotNet)]
    [Theory]
    public void ImplementationFirst_AutomaticallyBinds_TimeSpan_With_Format(
        DurationFormat format)
    {
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType(new DurationType(format: format))
            .Create()
            .MakeExecutable()
            .Execute("{ duration }")
            .ToJson()
            .MatchSnapshot(postFix: format);
    }

    [Fact]
    public void ImplementationFirst_Different_TimeSpan_Formats_In_Same_Type()
    {
        SchemaBuilder.New()
            .AddQueryType<QueryWithTwoDurations>()
            .AddType(new DurationType(format: DurationFormat.DotNet))
            .AddType(new DurationType(
                "IsoTimeSpan",
                format: DurationFormat.Iso8601,
                bind: BindingBehavior.Explicit))
            .Create()
            .MakeExecutable()
            .Execute("{ duration1 duration2 }")
            .ToJson()
            .MatchSnapshot();
    }

    public class Query
    {
        public TimeSpan Duration() => TimeSpan.FromDays(1);
    }

    public class QueryWithTwoDurations
    {
        public TimeSpan Duration1() => TimeSpan.FromDays(1);

        [IsoTimeSpan]
        public TimeSpan Duration2() => TimeSpan.FromDays(1);
    }

    private sealed class IsoTimeSpanAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
        {
            descriptor.Extend().OnBeforeCreate(
                d => d.Type = new SyntaxTypeReference(
                    new NamedTypeNode("IsoTimeSpan"),
                    TypeContext.Output));
        }
    }
}
