using HotChocolate.Language;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using System.Reflection;

namespace HotChocolate.Types;

public class TimeSpanTypeTests
{
    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "PT5M")]
    [InlineData(TimeSpanFormat.DotNet, "00:05:00")]
    public void Serialize_TimeSpan(TimeSpanFormat format, string expectedValue)
    {
        // arrange
        var timeSpanType = new TimeSpanType(format);
        var timeSpan = TimeSpan.FromMinutes(5);

        // act
        var serializedValue = (string)timeSpanType.Serialize(timeSpan);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "P10675199DT2H48M5.4775807S")]
    [InlineData(TimeSpanFormat.DotNet, "10675199.02:48:05.4775807")]
    public void Serialize_TimeSpan_Max(TimeSpanFormat format, string expectedValue)
    {
        // arrange
        var timeSpanType = new TimeSpanType(format);
        var timeSpan = TimeSpan.MaxValue;

        // act
        var serializedValue = (string)timeSpanType.Serialize(timeSpan);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "-P10675199DT2H48M5.4775808S")]
    [InlineData(TimeSpanFormat.DotNet, "-10675199.02:48:05.4775808")]
    public void Serialize_TimeSpan_Min(TimeSpanFormat format, string expectedValue)
    {
        // arrange
        var timeSpanType = new TimeSpanType(format);
        var timeSpan = TimeSpan.MinValue;

        // act
        var serializedValue = (string)timeSpanType.Serialize(timeSpan);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_TimeSpan_DefaultFormat()
    {
        // arrange
        var timeSpanType = new TimeSpanType();
        var timeSpan = TimeSpan.FromMinutes(5);
        var expectedValue = "PT5M";

        // act
        var serializedValue = (string)timeSpanType.Serialize(timeSpan);

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    public void Serialize_Null()
    {
        // arrange
        var timeSpanType = new TimeSpanType();

        // act
        var serializedValue = timeSpanType.Serialize(null);

        // assert
        Assert.Null(serializedValue);
    }

    [Fact]
    public void Serialize_String_Exception()
    {
        // arrange
        var timeSpanType = new TimeSpanType();

        // act
        Action a = () => timeSpanType.Serialize("bad");

        // assert
        Assert.Throws<SerializationException>(a);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "PT5M")]
    [InlineData(TimeSpanFormat.DotNet, "00:05:00")]
    public void ParseLiteral_StringValueNode(TimeSpanFormat format, string literalValue)
    {
        // arrange
        var timeSpanType = new TimeSpanType(format);
        var literal = new StringValueNode(literalValue);
        var expectedTimeSpan = TimeSpan.FromMinutes(5);

        // act
        var timeSpan = (TimeSpan)timeSpanType
            .ParseLiteral(literal);

        // assert
        Assert.Equal(expectedTimeSpan, timeSpan);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "PT5M")]
    [InlineData(TimeSpanFormat.DotNet, "00:05:00")]
    public void Deserialize_TimeSpan(TimeSpanFormat format, string actualValue)
    {
        // arrange
        var timeSpanType = new TimeSpanType(format);
        var timeSpan = TimeSpan.FromMinutes(5);

        // act
        var deserializedValue = (TimeSpan)timeSpanType
            .Deserialize(actualValue);

        // assert
        Assert.Equal(timeSpan, deserializedValue);
    }

    [Fact]
    public void Deserialize_TimeSpan_Weeks()
    {
        // arrange
        var timeSpanType = new TimeSpanType();
        var timeSpan = TimeSpan.FromDays(79);

        // act
        var deserializedValue = (TimeSpan)timeSpanType
            .Deserialize("P2M2W5D");

        // assert
        Assert.Equal(timeSpan, deserializedValue);
    }

    [Fact]
    public void Deserialize_TimeSpan_CannotEndWithDigits()
    {
        // arrange
        var timeSpanType = new TimeSpanType();

        // act
        var success = timeSpanType
            .TryDeserialize("PT5", out var deserialized);

        // assert
        Assert.False(success);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "P10675199DT2H48M5.4775807S")]
    [InlineData(TimeSpanFormat.DotNet, "10675199.02:48:05.4775807")]
    public void Deserialize_TimeSpan_Max(TimeSpanFormat format, string actualValue)
    {
        // arrange
        var timeSpanType = new TimeSpanType(format);
        var timeSpan = TimeSpan.MaxValue;

        // act
        var deserializedValue = (TimeSpan)timeSpanType
            .Deserialize(actualValue);

        // assert
        Assert.Equal(timeSpan, deserializedValue);
    }

    [Theory]
    [InlineData(TimeSpanFormat.Iso8601, "-P10675199DT2H48M5.4775808S")]
    [InlineData(TimeSpanFormat.DotNet, "-10675199.02:48:05.4775808")]
    public void Deserialize_TimeSpan_Min(TimeSpanFormat format, string actualValue)
    {
        // arrange
        var timeSpanType = new TimeSpanType(format);
        var timeSpan = TimeSpan.MinValue;

        // act
        var deserializedValue = (TimeSpan)timeSpanType
            .Deserialize(actualValue);

        // assert
        Assert.Equal(timeSpan, deserializedValue);
    }

    [Fact]
    public void Deserialize_InvalidString()
    {
        // arrange
        var timeSpanType = new TimeSpanType();

        // act
        var success = timeSpanType
            .TryDeserialize("bad", out var deserialized);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void Deserialize_Null_To_Null()
    {
        // arrange
        var timeSpanType = new TimeSpanType();

        // act
        var success = timeSpanType
            .TryDeserialize(null, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    public void ParseLiteral_NullValueNode()
    {
        // arrange
        var timeSpanType = new TimeSpanType();
        var literal = NullValueNode.Default;

        // act
        var value = timeSpanType.ParseLiteral(literal);

        // assert
        Assert.Null(value);
    }

    [Fact]
    public void ParseValue_Null()
    {
        // arrange
        var timeSpanType = new TimeSpanType();

        // act
        var literal = timeSpanType.ParseValue(null);

        // assert
        Assert.IsType<NullValueNode>(literal);
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
            MemberInfo member)
        {
            descriptor.Extend().OnBeforeCreate(
                d => d.Type = new SyntaxTypeReference(
                    new NamedTypeNode("IsoTimeSpan"),
                    TypeContext.Output));
        }
    }
}
