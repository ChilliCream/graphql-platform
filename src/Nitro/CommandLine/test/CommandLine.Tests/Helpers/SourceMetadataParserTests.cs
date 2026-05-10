using System.Text.Json;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Helpers;

public sealed class SourceMetadataParserTests
{
    private static readonly JsonSerializerOptions s_serializerOptions = new() { WriteIndented = true };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_Should_ReturnNull_When_JsonIsNullOrWhitespace(string? json)
    {
        // act
        var result = SourceMetadataParser.Parse(json);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_Should_ReturnGitHubMetadata_When_TypeIsGitHub()
    {
        // arrange
        const string json =
            """
            {
              "type": "github",
              "repositoryUrl": "https://github.com/ChilliCream/fusion-demo",
              "commitHash": "a91ee680fb675cbd37cf9832de8d0c6bb7327618",
              "actor": "michaelstaib",
              "runId": "22179586464",
              "runNumber": "30",
              "workflowName": "Release",
              "jobId": "64137235633"
            }
            """;

        // act
        var result = SourceMetadataParser.Parse(json);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """
            {
              "GitHub": {
                "Actor": "michaelstaib",
                "CommitHash": "a91ee680fb675cbd37cf9832de8d0c6bb7327618",
                "WorkflowName": "Release",
                "RunNumber": "30",
                "RunId": "22179586464",
                "JobId": "64137235633",
                "RepositoryUrl": "https://github.com/ChilliCream/fusion-demo"
              },
              "AzureDevOps": null
            }
            """);
    }

    [Fact]
    public void Parse_Should_DefaultToGitHubMetadata_When_TypeMissing()
    {
        // arrange
        const string json =
            """
            {
              "repositoryUrl": "https://github.com/ChilliCream/fusion-demo",
              "commitHash": "a91ee680fb675cbd37cf9832de8d0c6bb7327618",
              "actor": "michaelstaib",
              "runId": "22179586464",
              "runNumber": "30",
              "workflowName": "Release",
              "jobId": "64137235633"
            }
            """;

        // act
        var result = SourceMetadataParser.Parse(json);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """
            {
              "GitHub": {
                "Actor": "michaelstaib",
                "CommitHash": "a91ee680fb675cbd37cf9832de8d0c6bb7327618",
                "WorkflowName": "Release",
                "RunNumber": "30",
                "RunId": "22179586464",
                "JobId": "64137235633",
                "RepositoryUrl": "https://github.com/ChilliCream/fusion-demo"
              },
              "AzureDevOps": null
            }
            """);
    }

    [Fact]
    public void Parse_Should_ReturnGitHubMetadata_When_OptionalJobIdMissing()
    {
        // arrange
        const string json =
            """
            {
              "type": "github",
              "repositoryUrl": "https://github.com/ChilliCream/fusion-demo",
              "commitHash": "a91ee680fb675cbd37cf9832de8d0c6bb7327618",
              "actor": "michaelstaib",
              "runId": "22179586464",
              "runNumber": "30",
              "workflowName": "Release"
            }
            """;

        // act
        var result = SourceMetadataParser.Parse(json);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """
            {
              "GitHub": {
                "Actor": "michaelstaib",
                "CommitHash": "a91ee680fb675cbd37cf9832de8d0c6bb7327618",
                "WorkflowName": "Release",
                "RunNumber": "30",
                "RunId": "22179586464",
                "JobId": null,
                "RepositoryUrl": "https://github.com/ChilliCream/fusion-demo"
              },
              "AzureDevOps": null
            }
            """);
    }

    [Fact]
    public void Parse_Should_ReturnAzureDevOpsMetadata_When_TypeIsAzureDevOps()
    {
        // arrange
        const string json =
            """
            {
              "type": "azureDevOps",
              "actor": { "name": "Ada Lovelace", "email": "ada.lovelace@example.com" },
              "pipelineName": "My Custom Pipeline",
              "runNumber": "20260510T164405Z",
              "runId": "12",
              "jobId": "12f1170f-54f2-53f3-20dd-22fc7dff55f9",
              "taskId": "97a08166-1084-5e0c-e6c0-57facfef6992",
              "projectUrl": "https://dev.azure.com/contoso/testing",
              "commitHash": "367ef3ac3e63f0421b3e507e557bbcc320c8d1d0",
              "repositoryUrl": "https://dev.azure.com/contoso/testing/_git/testing"
            }
            """;

        // act
        var result = SourceMetadataParser.Parse(json);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """
            {
              "GitHub": null,
              "AzureDevOps": {
                "Actor": {
                  "Name": "Ada Lovelace",
                  "Email": "ada.lovelace@example.com"
                },
                "PipelineName": "My Custom Pipeline",
                "RunNumber": "20260510T164405Z",
                "RunId": "12",
                "ProjectUrl": "https://dev.azure.com/contoso/testing",
                "CommitHash": "367ef3ac3e63f0421b3e507e557bbcc320c8d1d0",
                "JobId": "12f1170f-54f2-53f3-20dd-22fc7dff55f9",
                "TaskId": "97a08166-1084-5e0c-e6c0-57facfef6992",
                "RepositoryUrl": "https://dev.azure.com/contoso/testing/_git/testing"
              }
            }
            """);
    }

    [Fact]
    public void Parse_Should_ReturnAzureDevOpsMetadata_When_OnlyRequiredFieldsPresent()
    {
        // arrange
        const string json =
            """
            {
              "type": "azureDevOps",
              "actor": { "name": "Ada Lovelace" },
              "pipelineName": "My Custom Pipeline",
              "runNumber": "20260510T164405Z",
              "runId": "12",
              "projectUrl": "https://dev.azure.com/contoso/testing"
            }
            """;

        // act
        var result = SourceMetadataParser.Parse(json);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """
            {
              "GitHub": null,
              "AzureDevOps": {
                "Actor": {
                  "Name": "Ada Lovelace",
                  "Email": null
                },
                "PipelineName": "My Custom Pipeline",
                "RunNumber": "20260510T164405Z",
                "RunId": "12",
                "ProjectUrl": "https://dev.azure.com/contoso/testing",
                "CommitHash": null,
                "JobId": null,
                "TaskId": null,
                "RepositoryUrl": null
              }
            }
            """);
    }

    [Fact]
    public void Parse_Should_Throw_When_TypeIsUnsupported()
    {
        // arrange
        const string json =
            """
            {
              "type": "gitlab"
            }
            """;

        // act
        static void Act() => SourceMetadataParser.Parse(json);

        // assert
        Assert.Throws<ExitException>(Act).Message.MatchInlineSnapshot(
            "Failed to parse --source-metadata: unsupported 'type' value 'gitlab'. "
            + "Expected 'github' or 'azureDevOps'.");
    }

    [Fact]
    public void Parse_Should_Throw_When_TypeIsNotAString()
    {
        // arrange
        const string json =
            """
            {
              "type": 42
            }
            """;

        // act
        static void Act() => SourceMetadataParser.Parse(json);

        // assert
        Assert.Throws<ExitException>(Act).Message.MatchInlineSnapshot(
            "Failed to parse --source-metadata: 'type' must be a string. "
            + "Expected 'github' or 'azureDevOps'.");
    }

    [Fact]
    public void Parse_Should_Throw_When_RootIsNotAnObject()
    {
        // arrange
        const string json = "[]";

        // act
        static void Act() => SourceMetadataParser.Parse(json);

        // assert
        Assert.Throws<ExitException>(Act).Message.MatchInlineSnapshot(
            "Failed to parse --source-metadata: expected a JSON object.");
    }

    [Fact]
    public void Parse_Should_Throw_When_RequiredGitHubPropertyMissing()
    {
        // arrange
        const string json =
            """
            {
              "type": "github",
              "actor": "michaelstaib"
            }
            """;

        // act
        static void Act() => SourceMetadataParser.Parse(json);

        // assert
        Assert.Throws<ExitException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_RequiredAzureDevOpsPropertyMissing()
    {
        // arrange
        const string json =
            """
            {
              "type": "azureDevOps",
              "actor": { "name": "Ada Lovelace" }
            }
            """;

        // act
        static void Act() => SourceMetadataParser.Parse(json);

        // assert
        Assert.Throws<ExitException>(Act);
    }

    [Fact]
    public void Parse_Should_Throw_When_JsonIsInvalid()
    {
        // arrange
        const string json = "{ not json";

        // act
        static void Act() => SourceMetadataParser.Parse(json);

        // assert
        Assert.Throws<ExitException>(Act);
    }

    private static string Serialize(SourceMetadata? metadata)
        => JsonSerializer.Serialize(metadata, s_serializerOptions);
}
