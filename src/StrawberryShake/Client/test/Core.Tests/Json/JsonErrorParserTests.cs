using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace StrawberryShake.Json
{
    public class JsonErrorParserTests
    {
        [Fact]
        public void Error_With_Message()
        {
            // arrange
            var result = JsonDocument.Parse(@"{ ""errors"": [ { ""message"": ""errors"" } ] }");

            // act
            IReadOnlyList<IClientError>? errors = JsonErrorParser.ParseErrors(result.RootElement);

            // assert
            Assert.Collection(errors!, error => Assert.Equal("errors", error.Message));
        }

        [Fact]
        public void Error_Has_No_Message()
        {
            // arrange
            var result = JsonDocument.Parse(@"
            {
                ""errors"": [
                    {
                    }
                ]
            }");

            // act
            IReadOnlyList<IClientError>? errors = JsonErrorParser.ParseErrors(result.RootElement);

            // assert
            Assert.Collection(
                errors!,
                error => Assert.Equal(
                    "The error format is invalid and was missing the property `message`.",
                    error.Message));
        }

        [Fact]
        public void Error_With_Path()
        {
            // arrange
            var result = JsonDocument.Parse(@"
            {
                ""errors"": [
                    {
                        ""message"": ""errors"",
                        ""path"": [ 1, ""foo"", 2, ""bar"" ]
                    }
                ]
            }");

            // act
            IReadOnlyList<IClientError>? errors = JsonErrorParser.ParseErrors(result.RootElement);

            // assert
            Assert.Collection(
                errors!,
                error =>
                {
                    Assert.Equal("errors", error.Message);
                    Assert.Collection(
                        error.Path!,
                        element => Assert.Equal(1, Assert.IsType<int>(element)),
                        element => Assert.Equal("foo", Assert.IsType<string>(element)),
                        element => Assert.Equal(2, Assert.IsType<int>(element)),
                        element => Assert.Equal("bar", Assert.IsType<string>(element)));
                });
        }

        [Fact]
        public void Error_With_Path_With_Invalid_Path_Value()
        {
            // arrange
            var result = JsonDocument.Parse(@"
            {
                ""errors"": [
                    {
                        ""message"": ""errors"",
                        ""path"": [ true ]
                    }
                ]
            }");

            // act
            IReadOnlyList<IClientError>? errors = JsonErrorParser.ParseErrors(result.RootElement);

            // assert
            Assert.Collection(
                errors!,
                error =>
                {
                    Assert.Equal("errors", error.Message);
                    Assert.Collection(
                        error.Path!,
                        element => Assert.Equal(
                            "NOT_SUPPORTED_VALUE",
                            Assert.IsType<string>(element)));
                });
        }

        [Fact]
        public void Error_With_Locations()
        {
            // arrange
            var result = JsonDocument.Parse(@"
            {
                ""errors"": [
                    {
                        ""message"": ""errors"",
                        ""locations"": [ { ""line"": 1, ""column"": 5 } ]
                    }
                ]
            }");

            // act
            IReadOnlyList<IClientError>? errors = JsonErrorParser.ParseErrors(result.RootElement);

            // assert
            Assert.Collection(
                errors!,
                error =>
                {
                    Assert.Equal("errors", error.Message);
                    Assert.Collection(
                        error.Locations!,
                        location =>
                        {
                            Assert.Equal(1, location.Line);
                            Assert.Equal(5, location.Column);
                        });
                });
        }
    }
}
