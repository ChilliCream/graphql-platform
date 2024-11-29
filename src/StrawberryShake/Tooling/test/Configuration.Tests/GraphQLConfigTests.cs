using Xunit;

namespace StrawberryShake.Tools.Configuration;

public class GraphQLConfigTests
{
    [Fact]
    public void Save_Default_Config()
    {
        new GraphQLConfig()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void Load_Json_Is_Null() =>
        Assert.Throws<ArgumentException>(
            () => GraphQLConfig.FromJson(null!));

    [Fact]
    public void Load_Json()
    {
        GraphQLConfig.FromJson(@"{
                ""schema"": ""schema.graphql"",
                ""documents"": ""**/*.graphql"",
                ""extensions"": {
                        ""strawberryShake"": {
                        ""name"": ""Client"",
                        ""accessModifier"": ""public"",
                        ""dependencyInjection"": true,
                        ""strictSchemaValidation"": true,
                        ""hashAlgorithm"": ""md5"",
                        ""useSingleFile"": true,
                        ""requestStrategy"": ""Default"",
                        ""outputDirectoryName"": ""Generated"",
                        ""noStore"": false,
                        ""emitGeneratedCode"": true,
                        ""records"": {
                            ""inputs"": false,
                            ""entities"": false
                        },
                        ""transportProfiles"": [
                            {
                            ""default"": ""Http"",
                            ""subscription"": ""WebSocket""
                            }]
                        }
                    }
                }
                ").MatchSnapshot();
    }

    [Fact]
    public void Load_Json_With_Transport_Profiles()
    {
        GraphQLConfig.FromJson(@"{
                ""schema"": ""schema.graphql"",
                ""documents"": ""**/*.graphql"",
                ""extensions"": {
                        ""strawberryShake"": {
                        ""name"": ""Client"",
                        ""accessModifier"": ""public"",
                        ""dependencyInjection"": true,
                        ""strictSchemaValidation"": true,
                        ""hashAlgorithm"": ""md5"",
                        ""useSingleFile"": true,
                        ""requestStrategy"": ""Default"",
                        ""outputDirectoryName"": ""Generated"",
                        ""noStore"": false,
                        ""emitGeneratedCode"": true,
                        ""records"": {
                            ""inputs"": false,
                            ""entities"": false
                        },
                        ""transportProfiles"": [
                            {
                                ""default"": ""Http"",
                            },
                            {
                                ""default"": ""WebSocket""
                            }]
                        }
                    }
                }
                ").MatchSnapshot();
    }

    [Fact]
    public void Load_Json_With_Records()
    {
        GraphQLConfig.FromJson(@"{
                ""schema"": ""schema.graphql"",
                ""documents"": ""**/*.graphql"",
                ""extensions"": {
                        ""strawberryShake"": {
                        ""name"": ""Client"",
                        ""accessModifier"": ""public"",
                        ""dependencyInjection"": true,
                        ""strictSchemaValidation"": true,
                        ""hashAlgorithm"": ""md5"",
                        ""useSingleFile"": true,
                        ""requestStrategy"": ""Default"",
                        ""outputDirectoryName"": ""Generated"",
                        ""noStore"": false,
                        ""emitGeneratedCode"": true,
                        ""records"": {
                            ""inputs"": true,
                            ""entities"": true
                        },
                        ""transportProfiles"": [
                            {
                            ""default"": ""Http"",
                            ""subscription"": ""WebSocket""
                            }]
                        }
                    }
                }
                ").MatchSnapshot();
    }

    [Fact]
    public void Load_Json_With_Documents_Array()
    {
        GraphQLConfig.FromJson(@"{
                ""schema"": ""schema.graphql"",
                ""documents"": [""**/*.graphql"", ""**/*.graphqls""],
                ""extensions"": {
                        ""strawberryShake"": {
                        ""name"": ""Client"",
                        ""accessModifier"": ""public"",
                        ""dependencyInjection"": true,
                        ""strictSchemaValidation"": true,
                        ""hashAlgorithm"": ""md5"",
                        ""useSingleFile"": true,
                        ""requestStrategy"": ""Default"",
                        ""outputDirectoryName"": ""Generated"",
                        ""noStore"": false,
                        ""emitGeneratedCode"": true,
                        ""records"": {
                            ""inputs"": true,
                            ""entities"": true
                        },
                        ""transportProfiles"": [
                            {
                            ""default"": ""Http"",
                            ""subscription"": ""WebSocket""
                            }]
                        }
                    }
                }
                ").MatchSnapshot();
    }
}
