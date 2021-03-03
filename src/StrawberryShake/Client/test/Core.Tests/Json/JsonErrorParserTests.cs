using System.Collections.Generic;
using System.Text.Json;
using Snapshooter.Xunit;
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

        [Fact]
        public void Error_With_Extensions()
        {
            // arrange
            var result = JsonDocument.Parse(@"
            {
                ""errors"": [
                    {
                        ""message"": ""errors"",
                        ""extensions"":
                        {
                            ""s"": ""abc"",
                            ""i"": 5,
                            ""f"": 1.5,
                            ""true"": true,
                            ""false"": false,
                            ""null"": null,
                            ""il"": [ 1, 2, 3 ],
                            ""sl"": [ ""a"", ""b"" ],
                            ""ol"": [ { ""s"": ""abc"" } ]
                        }
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
                    error.Extensions.MatchSnapshot();
                });
        }

        [Fact]
        public void Error_With_Extensions_Code()
        {
            // arrange
            var result = JsonDocument.Parse(@"
            {
                ""errors"": [
                    {
                        ""message"": ""errors"",
                        ""extensions"":
                        {
                          ""code"": ""CS1234""
                        }
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
                    Assert.Equal("CS1234", error.Code);
                    error.Extensions.MatchSnapshot();
                });
        }

        [Fact]
        public void Error_With_Code()
        {
            // arrange
            var result = JsonDocument.Parse(@"
            {
                ""errors"": [
                    {
                        ""message"": ""errors"",
                        ""code"": ""CS1234""
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
                    Assert.Equal("CS1234", error.Code);
                });
        }

        [Fact]
        public void Error_With_Root_Code_Takes_Preference()
        {
            // arrange
            var result = JsonDocument.Parse(@"
            {
                ""errors"": [
                    {
                        ""message"": ""errors"",
                        ""code"": ""CSROOT"",
                        ""extensions"":
                        {
                          ""code"": ""CS1234""
                        }
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
                    Assert.Equal("CSROOT", error.Code);
                    error.Extensions.MatchSnapshot();
                });
        }

        [Fact]
        public void Parsing_Error()
        {
            // arrange
            var result = JsonDocument.Parse(@"
            {
                ""errors"": [
                    {
                        ""message"": ""errors"",
                        ""locations"": [ { ""column"": 5 } ]
                    },
                    {
                        ""message"": ""errors"",
                        ""locations"": [ { ""column"": 5 } ]
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
                    Assert.Equal("Error parsing a server error.", error.Message);
                    Assert.NotNull(error.Exception);
                },
                error =>
                {
                    Assert.Equal("Error parsing a server error.", error.Message);
                    Assert.NotNull(error.Exception);
                });
        }

        [Fact]
        public void Parsing_Errors_Does_Not_Exist()
        {
            // arrange
            var result = JsonDocument.Parse(@"
            {
            }");

            // act
            IReadOnlyList<IClientError>? errors = JsonErrorParser.ParseErrors(result.RootElement);

            // assert
            Assert.Null(errors);
        }

        [Fact]
        public void Parsing_Errors_Is_Null()
        {
            // arrange
            var result = JsonDocument.Parse(@"
            {
                ""errors"": null
            }");

            // act
            IReadOnlyList<IClientError>? errors = JsonErrorParser.ParseErrors(result.RootElement);

            // assert
            Assert.Null(errors);
        }
    }
}
