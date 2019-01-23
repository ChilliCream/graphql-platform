using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace HotChocolate.Execution.Utilities
{
    public class JsonWriterTests
    {
        [Fact]
        static public void WriteBasicString()
        {
            // Arrange
            var basicString = "Using test string data.";

            var jsonString = $"\"{basicString}\"";
            using (var memStream = new MemoryStream())
            {
                // Act
                JsonWriter.WriteValue(basicString, memStream);

                // Assert
                Assert.Equal(
                    Encoding.UTF8.GetString(memStream.ToArray()),
                    jsonString);
            }
        }

        [Theory]
        [InlineData("This\r\nIs\r\nA\r\nNew\r\nLine", @"This\r\nIs\r\nA\r\nNew\r\nLine")]
        [InlineData("I am profo\bund", @"I am profo\bund")]
        [InlineData("\\\\\\\\\\\\\\\\\\", @"\\\\\\\\\\\\\\\\\\")]
        [InlineData("//////////////////", @"\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/")]
        [InlineData("This\fA\t\t\t\tFourTabs\f\f\f\fFourFeeds", @"This\fA\t\t\t\tFourTabs\f\f\f\fFourFeeds")]
        [InlineData("\u0061", @"a")]
        [InlineData("\\/\\//\\/\\//\\/\\//\\/\\//", @"\\\/\\\/\/\\\/\\\/\/\\\/\\\/\/\\\/\\\/\/")]
        static public void WriteComplexString(string input, string expectedValue)
        {
            // Arrange
            var jsonString = $"\"{expectedValue}\"";
            using (var memStream = new MemoryStream())
            {
                // Act
                JsonWriter.WriteValue(input, memStream);

                // Assert
                Assert.Equal(
                    Encoding.UTF8.GetString(memStream.ToArray()),
                    jsonString);
            }
        }

        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        static public void WriteBool(bool value, string expectedValue)
        {
            using (var memStream = new MemoryStream())
            {
                // Act
                JsonWriter.WriteValue(value, memStream);

                // Assert
                Assert.Equal(
                    Encoding.UTF8.GetString(memStream.ToArray()),
                    expectedValue);
            }
        }

        [Theory]
        [InlineData(5, "5")]
        [InlineData(10, "10")]
        static public void WriteIntValue(int value, string expectedValue)
        {
            using (var memStream = new MemoryStream())
            {
                // Act
                JsonWriter.WriteValue(value, memStream);

                // Assert
                Assert.Equal(
                    Encoding.UTF8.GetString(memStream.ToArray()),
                    expectedValue);
            }
        }

        [Theory]
        [InlineData(5, "5")]
        [InlineData(10, "10")]
        static public void WriteUintValue(uint value, string expectedValue)
        {
            using (var memStream = new MemoryStream())
            {
                // Act
                JsonWriter.WriteValue(value, memStream);

                // Assert
                Assert.Equal(
                    Encoding.UTF8.GetString(memStream.ToArray()),
                    expectedValue);
            }
        }

        [Theory]
        [InlineData(5, "5")]
        [InlineData(10, "10")]
        static public void WriteLongValue(long value, string expectedValue)
        {
            using (var memStream = new MemoryStream())
            {
                // Act
                JsonWriter.WriteValue(value, memStream);

                // Assert
                Assert.Equal(
                    Encoding.UTF8.GetString(memStream.ToArray()),
                    expectedValue);
            }
        }

        [Theory]
        [InlineData(5, "5")]
        [InlineData(10, "10")]
        static public void WriteUlongValue(ulong value, string expectedValue)
        {
            using (var memStream = new MemoryStream())
            {
                // Act
                JsonWriter.WriteValue(value, memStream);

                // Assert
                Assert.Equal(
                    Encoding.UTF8.GetString(memStream.ToArray()),
                    expectedValue);
            }
        }

        [Theory]
        [InlineData(5.123, "5.123")]
        [InlineData(10.123, "10.123")]
        static public void WriteFloatValue(float value, string expectedValue)
        {
            using (var memStream = new MemoryStream())
            {
                // Act
                JsonWriter.WriteValue(value, memStream);

                // Assert
                Assert.Equal(
                    Encoding.UTF8.GetString(memStream.ToArray()),
                    expectedValue);
            }
        }

        [Theory]
        [InlineData(5.123, "5.123")]
        [InlineData(10.123, "10.123")]
        static public void WriteDoubleValue(double value, string expectedValue)
        {
            using (var memStream = new MemoryStream())
            {
                // Act
                JsonWriter.WriteValue(value, memStream);

                // Assert
                Assert.Equal(
                    Encoding.UTF8.GetString(memStream.ToArray()),
                    expectedValue);
            }
        }

        [Theory]
        [InlineData("5.1234567891023123")]
        [InlineData("10.01010101010101")]
        static public void WriteDecimalValue(string expectedValue)
        {
            var decimalValue = decimal.Parse(expectedValue);
            using (var memStream = new MemoryStream())
            {
                // Act
                JsonWriter.WriteValue(decimalValue, memStream);

                // Assert
                Assert.Equal(
                    Encoding.UTF8.GetString(memStream.ToArray()),
                    expectedValue);
            }
        }
    }
}
