using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types.Descriptors;
using System.Reflection;

namespace HotChocolate.Types;

public class TimeSpanTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new TimeSpanType();

        // assert
        Assert.Equal("TimeSpan", type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new TimeSpanType();
        var literal = new StringValueNode("PT5M");
        var expectedTimeSpan = TimeSpan.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedTimeSpan, runtimeValue);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "PT5M")]
    [InlineData(TimeSpanFormat.DotNet, "00:05:00")]
    public void CoerceInputLiteral_WithFormat(TimeSpanFormat format, string literalValue)
    {
        // arrange
        var type = new TimeSpanType(format);
        var literal = new StringValueNode(literalValue);
        var expectedTimeSpan = TimeSpan.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedTimeSpan, runtimeValue);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "P10675199DT2H48M5.4775807S")]
    [InlineData(TimeSpanFormat.DotNet, "10675199.02:48:05.4775807")]
    public void CoerceInputLiteral_MaxValue(TimeSpanFormat format, string literalValue)
    {
        // arrange
        var type = new TimeSpanType(format);
        var literal = new StringValueNode(literalValue);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(TimeSpan.MaxValue, runtimeValue);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "-P10675199DT2H48M5.4775808S")]
    [InlineData(TimeSpanFormat.DotNet, "-10675199.02:48:05.4775808")]
    public void CoerceInputLiteral_MinValue(TimeSpanFormat format, string literalValue)
    {
        // arrange
        var type = new TimeSpanType(format);
        var literal = new StringValueNode(literalValue);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(TimeSpan.MinValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_Weeks()
    {
        // arrange
        var type = new TimeSpanType();
        var literal = new StringValueNode("P2M2W5D");
        var expectedTimeSpan = TimeSpan.FromDays(79);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedTimeSpan, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new TimeSpanType();
        var literal = new StringValueNode("bad");

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputLiteral_CannotEndWithDigits()
    {
        // arrange
        var type = new TimeSpanType();
        var literal = new StringValueNode("PT5");

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new TimeSpanType();
        var inputValue = JsonDocument.Parse("\"PT5M\"").RootElement;
        var expectedTimeSpan = TimeSpan.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expectedTimeSpan, runtimeValue);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "PT5M")]
    [InlineData(TimeSpanFormat.DotNet, "00:05:00")]
    public void CoerceInputValue_WithFormat(TimeSpanFormat format, string value)
    {
        // arrange
        var type = new TimeSpanType(format);
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
        var type = new TimeSpanType();
        var inputValue = JsonDocument.Parse("\"bad\"").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new TimeSpanType();
        var runtimeValue = TimeSpan.FromMinutes(5);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"PT5M\"");
    }

    [Fact]
    public void CoerceOutputValue_WithFormat_Iso8601()
    {
        // arrange
        var type = new TimeSpanType(TimeSpanFormat.Iso8601);
        var runtimeValue = TimeSpan.FromMinutes(5);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"PT5M\"");
    }

    [Fact]
    public void CoerceOutputValue_WithFormat_DotNet()
    {
        // arrange
        var type = new TimeSpanType(TimeSpanFormat.DotNet);
        var runtimeValue = TimeSpan.FromMinutes(5);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"00:05:00\"");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new TimeSpanType();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue("bad", resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral()
    {
        // arrange
        var type = new TimeSpanType();
        var runtimeValue = TimeSpan.FromMinutes(5);

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal("PT5M", Assert.IsType<StringValueNode>(literal).Value);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "PT5M")]
    [InlineData(TimeSpanFormat.DotNet, "00:05:00")]
    public void ValueToLiteral_WithFormat(TimeSpanFormat format, string expectedValue)
    {
        // arrange
        var type = new TimeSpanType(format);
        var runtimeValue = TimeSpan.FromMinutes(5);

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(expectedValue, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "P10675199DT2H48M5.4775807S")]
    [InlineData(TimeSpanFormat.DotNet, "10675199.02:48:05.4775807")]
    public void ValueToLiteral_MaxValue(TimeSpanFormat format, string expectedValue)
    {
        // arrange
        var type = new TimeSpanType(format);
        var runtimeValue = TimeSpan.MaxValue;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(expectedValue, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "-P10675199DT2H48M5.4775808S")]
    [InlineData(TimeSpanFormat.DotNet, "-10675199.02:48:05.4775808")]
    public void ValueToLiteral_MinValue(TimeSpanFormat format, string expectedValue)
    {
        // arrange
        var type = new TimeSpanType(format);
        var runtimeValue = TimeSpan.MinValue;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(expectedValue, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new TimeSpanType();
        var literal = new StringValueNode("PT5M");
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
        var type = new TimeSpanType();

        // act
        void Action() => type.CoerceInputLiteral(new IntValueNode(123));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void PureCodeFirst_AutomaticallyBinds_TimeSpan()
    {
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [InlineData(TimeSpanFormat.Iso8601)]
    [InlineData(TimeSpanFormat.DotNet)]
    [Theory]
    public void PureCodeFirst_AutomaticallyBinds_TimeSpan_With_Format(
        TimeSpanFormat format)
    {
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType(new TimeSpanType(format: format))
            .Create()
            .MakeExecutable()
            .Execute("{ duration }")
            .ToJson()
            .MatchSnapshot(postFix: format);
    }

    [Fact]
    public void PureCodeFirst_Different_TimeSpan_Formats_In_Same_Type()
    {
        SchemaBuilder.New()
            .AddQueryType<QueryWithTwoDurations>()
            .AddType(new TimeSpanType(format: TimeSpanFormat.DotNet))
            .AddType(new TimeSpanType(
                "IsoTimeSpan",
                format: TimeSpanFormat.Iso8601,
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
