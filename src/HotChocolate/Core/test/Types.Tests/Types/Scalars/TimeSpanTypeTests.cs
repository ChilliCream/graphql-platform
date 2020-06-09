using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class TimeSpanTypeTests
    {
        [Theory]
        [InlineData(TimeSpanFormat.ISO_8601, "PT5M")]
        [InlineData(TimeSpanFormat.DOT_NET, "00:05:00")]
        public void Serialize_TimeSpan(TimeSpanFormat format, string expectedValue)
        {
            // arrange
            var timeSpanType = new TimeSpanType(format);
            var timeSpan = TimeSpan.FromMinutes(5);

            // act
            string serializedValue = (string)timeSpanType.Serialize(timeSpan);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Theory]
        [InlineData(TimeSpanFormat.ISO_8601, "P10675199DT2H48M5.4775807S")]
        [InlineData(TimeSpanFormat.DOT_NET, "10675199.02:48:05.4775807")]
        public void Serialize_TimeSpan_Max(TimeSpanFormat format, string expectedValue)
        {
            // arrange
            var timeSpanType = new TimeSpanType(format);
            TimeSpan timeSpan = TimeSpan.MaxValue;

            // act
            string serializedValue = (string)timeSpanType.Serialize(timeSpan);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Theory]
        [InlineData(TimeSpanFormat.ISO_8601, "-P10675199DT2H48M5.4775808S")]
        [InlineData(TimeSpanFormat.DOT_NET, "-10675199.02:48:05.4775808")]
        public void Serialize_TimeSpan_Min(TimeSpanFormat format, string expectedValue)
        {
            // arrange
            var timeSpanType = new TimeSpanType(format);
            TimeSpan timeSpan = TimeSpan.MinValue;

            // act
            string serializedValue = (string)timeSpanType.Serialize(timeSpan);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_TimeSpan_DefaultFormat()
        {
            // arrange
            var timeSpanType = new TimeSpanType();
            var timeSpan = TimeSpan.FromMinutes(5);
            string expectedValue = "PT5M";

            // act
            string serializedValue = (string)timeSpanType.Serialize(timeSpan);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var timeSpanType = new TimeSpanType();

            // act
            object serializedValue = timeSpanType.Serialize(null);

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
            Assert.Throws<ScalarSerializationException>(a);
        }

        [Theory]
        [InlineData(TimeSpanFormat.ISO_8601, "PT5M")]
        [InlineData(TimeSpanFormat.DOT_NET, "00:05:00")]
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
        [InlineData(TimeSpanFormat.ISO_8601, "PT5M")]
        [InlineData(TimeSpanFormat.DOT_NET, "00:05:00")]
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

        [Theory]
        [InlineData(TimeSpanFormat.ISO_8601, "P10675199DT2H48M5.4775807S")]
        [InlineData(TimeSpanFormat.DOT_NET, "10675199.02:48:05.4775807")]
        public void Deserialize_TimeSpan_Max(TimeSpanFormat format, string actualValue)
        {
            // arrange
            var timeSpanType = new TimeSpanType(format);
            TimeSpan timeSpan = TimeSpan.MaxValue;

            // act
            var deserializedValue = (TimeSpan)timeSpanType
                .Deserialize(actualValue);

            // assert
            Assert.Equal(timeSpan, deserializedValue);
        }

        [Theory]
        [InlineData(TimeSpanFormat.ISO_8601, "-P10675199DT2H48M5.4775808S")]
        [InlineData(TimeSpanFormat.DOT_NET, "-10675199.02:48:05.4775808")]
        public void Deserialize_TimeSpan_Min(TimeSpanFormat format, string actualValue)
        {
            // arrange
            var timeSpanType = new TimeSpanType(format);
            TimeSpan timeSpan = TimeSpan.MinValue;

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
            bool success = timeSpanType
                .TryDeserialize("bad", out object deserialized);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void Deserialize_Null_To_Null()
        {
            // arrange
            var timeSpanType = new TimeSpanType();

            // act
            bool success = timeSpanType
                .TryDeserialize(null, out object deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var timeSpanType = new TimeSpanType();
            NullValueNode literal = NullValueNode.Default;

            // act
            object value = timeSpanType.ParseLiteral(literal);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            var timeSpanType = new TimeSpanType();

            // act
            IValueNode literal = timeSpanType.ParseValue(null);

            // assert
            Assert.IsType<NullValueNode>(literal);
        }
    }
}
