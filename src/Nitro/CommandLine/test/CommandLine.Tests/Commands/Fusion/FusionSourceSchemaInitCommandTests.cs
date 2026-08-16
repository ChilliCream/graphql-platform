using System.Text;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionSourceSchemaInitCommandTests(NitroCommandFixture fixture)
    : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a source schema settings file.

            Usage:
              nitro fusion source-schema init [options]

            Options:
              --name <name>                                     The name that identifies the source schema in the composite schema
              -f, --source-schema-file <source-schema-file>     The path to the source schema file (.graphqls) the settings belong to, or a directory containing it
              --settings-file <settings-file>                   The path to write the settings file to, instead of deriving it from the source schema file
              --url <url>                                       The URL the router uses to reach the source schema
              --dev-url <dev-url>                               The URL a local development environment uses to reach the source schema
              --client-name <client-name>                       The name of the HTTP client the router uses to reach the source schema
              --api-id <api-id>                                 The ID of the API [env: NITRO_API_ID]
              --kind <apollo-federation|generic|hot-chocolate>  The kind of GraphQL server that serves the source schema. When omitted, kind-specific settings are left unchanged
              --apollo-federation-version <1.0|2.0>             The Apollo Federation version the source schema is built with
              -w, --working-directory <working-directory>       Set the working directory for the command
              --cloud-url <cloud-url>                           The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>                               The API key or PAT used for authentication [env: NITRO_API_KEY]
              --output <json>                                   The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                    Show help and usage information

            Example:
              nitro fusion source-schema init \
                --name "products" \
                --source-schema-file ./products/schema.graphqls \
                --url "https://products.example.com/graphql"
            """);
    }

    [Fact]
    public async Task Init_WithSourceSchemaFile_WritesSettingsNextToSchema()
    {
        // arrange
        SetupNoAuthentication();
        SetupFile(SourceSchemaFile, "type Query { field: String! }");
        var capturedStream = SetupCreateFile(SourceSchemaSettingsFile);
        SetupDirectory("products");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products",
            "--source-schema-file",
            SourceSchemaFile,
            "--url",
            "https://products.example.com/graphql");

        // assert
        result.AssertSuccess(
            """
            Created '/some/working/directory/products/schema-settings.json'.
            """);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "https://products.example.com/graphql"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_WithSourceSchemaDirectory_WritesSettingsNextToSchemaInDirectory()
    {
        // arrange
        SetupNoAuthentication();
        SetupDirectory("products", "/some/working/directory/products/schema.graphqls");
        var capturedStream = SetupCreateFile(SourceSchemaSettingsFile);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products",
            "--source-schema-file",
            "products",
            "--url",
            "https://products.example.com/graphql");

        // assert
        result.AssertSuccess(
            """
            Created '/some/working/directory/products/schema-settings.json'.
            """);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "https://products.example.com/graphql"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_WithSourceSchemaDirectoryThatDoesNotExistYet_WritesSettingsIntoDirectory()
    {
        // arrange
        SetupNoAuthentication();
        var capturedStream = SetupCreateFile(SourceSchemaSettingsFile);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products",
            "--source-schema-file",
            "products",
            "--url",
            "https://products.example.com/graphql");

        // assert
        result.AssertSuccess(
            """
            Created '/some/working/directory/products/schema-settings.json'.
            """);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "https://products.example.com/graphql"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_WithSettingsFile_WritesSettingsToGivenPath()
    {
        // arrange
        SetupNoAuthentication();
        var capturedStream = SetupCreateFile("config/products.json");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products",
            "--source-schema-file",
            SourceSchemaFile,
            "--settings-file",
            "config/products.json",
            "--url",
            "https://products.example.com/graphql");

        // assert
        result.AssertSuccess(
            """
            Created '/some/working/directory/config/products.json'.
            """);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "https://products.example.com/graphql"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_WithSchemaExtensionsFile_ReturnsError()
    {
        // arrange
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products",
            "--source-schema-file",
            "products/schema-extensions.graphqls");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Schema extensions file '/some/working/directory/products/schema-extensions.graphqls' cannot be used as a source schema file. Provide the base schema file instead.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Init_WithoutSourceSchemaFile_WritesSettingsToWorkingDirectory()
    {
        // arrange
        SetupNoAuthentication();
        var capturedStream = SetupCreateFile("schema-settings.json");
        SetupDirectory("");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "reviews",
            "--url",
            "http://localhost:5000/graphql");

        // assert
        result.AssertSuccess(
            """
            Created '/some/working/directory/schema-settings.json'.
            """);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "reviews",
              "transports": {
                "http": {
                  "url": "http://localhost:5000/graphql"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_WithAllTransportOptions_WritesCompleteSettings()
    {
        // arrange
        SetupNoAuthentication();
        var capturedStream = SetupCreateFile("schema-settings.json");
        SetupDirectory("");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products",
            "--url",
            "{{API_URL}}",
            "--dev-url",
            "http://localhost:5110/graphql",
            "--client-name",
            "fusion",
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            Created '/some/working/directory/schema-settings.json'.
            """);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "{{API_URL}}",
                  "devUrl": "http://localhost:5110/graphql",
                  "clientName": "fusion"
                }
              },
              "extensions": {
                "nitro": {
                  "apiId": "api-1"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_WithHotChocolateKind_WritesTransportCapabilities()
    {
        // arrange
        SetupNoAuthentication();
        var capturedStream = SetupCreateFile("schema-settings.json");
        SetupDirectory("");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products",
            "--url",
            "http://localhost:5000/graphql",
            "--kind",
            "hot-chocolate");

        // assert
        result.AssertSuccess(
            """
            Created '/some/working/directory/schema-settings.json'.
            """);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "http://localhost:5000/graphql",
                  "capabilities": {
                    "batching": {
                      "variableBatching": true,
                      "requestBatching": true,
                      "aliasBatching": true
                    },
                    "onError": "propagate"
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_WithApolloFederationKind_WritesFederationSupport()
    {
        // arrange
        SetupNoAuthentication();
        var capturedStream = SetupCreateFile("schema-settings.json");
        SetupDirectory("");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products",
            "--url",
            "http://localhost:5000/graphql",
            "--kind",
            "apollo-federation",
            "--apollo-federation-version",
            "1.0");

        // assert
        result.AssertSuccess(
            """
            Created '/some/working/directory/schema-settings.json'.
            """);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "http://localhost:5000/graphql"
                }
              },
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "1.0"
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_WithApolloFederationKind_DefaultsToVersion2()
    {
        // arrange
        SetupNoAuthentication();
        var capturedStream = SetupCreateFile("schema-settings.json");
        SetupDirectory("");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products",
            "--url",
            "http://localhost:5000/graphql",
            "--kind",
            "apollo-federation");

        // assert
        result.AssertSuccess(
            """
            Created '/some/working/directory/schema-settings.json'.
            """);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "http://localhost:5000/graphql"
                }
              },
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "2.0"
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_ExistingSettings_UpdatesInPlaceAndPreservesUnknownSettings()
    {
        // arrange
        SetupNoAuthentication();
        SetupFile(
            "schema-settings.json",
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "http://localhost:5110/graphql",
                  "clientName": "fusion"
                }
              },
              "environments": {
                "development": {
                  "API_URL": "http://localhost:5110/graphql"
                }
              }
            }
            """);
        var capturedStream = SetupCreateFile("schema-settings.json");
        SetupDirectory("");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--url",
            "https://products.example.com/graphql");

        // assert
        result.AssertSuccess(
            """
            Updated '/some/working/directory/schema-settings.json'.
            """);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "https://products.example.com/graphql",
                  "clientName": "fusion"
                }
              },
              "environments": {
                "development": {
                  "API_URL": "http://localhost:5110/graphql"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_ExistingApolloFederationSettings_KeepsConfiguredVersion()
    {
        // arrange
        SetupNoAuthentication();
        SetupFile(
            "schema-settings.json",
            """
            {
              "name": "products",
              "transports": { "http": { "url": "http://localhost:5110/graphql" } },
              "extensions": {
                "chillicream": { "apolloFederationSupport": { "version": "1.0" } }
              }
            }
            """);
        var capturedStream = SetupCreateFile("schema-settings.json");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--kind",
            "apollo-federation",
            "--url",
            "https://products.example.com/graphql");

        // assert
        Assert.Equal(0, result.ExitCode);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "https://products.example.com/graphql"
                }
              },
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "1.0"
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_ChangingKindToGeneric_RemovesKindSpecificSettings()
    {
        // arrange
        SetupNoAuthentication();
        SetupFile(
            "schema-settings.json",
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "http://localhost:5110/graphql",
                  "capabilities": { "onError": "propagate" }
                }
              },
              "extensions": {
                "chillicream": { "apolloFederationSupport": { "version": "2.0" } },
                "nitro": { "apiId": "api-1" }
              }
            }
            """);
        var capturedStream = SetupCreateFile("schema-settings.json");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--kind",
            "generic");

        // assert
        Assert.Equal(0, result.ExitCode);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "http://localhost:5110/graphql"
                }
              },
              "extensions": {
                "nitro": {
                  "apiId": "api-1"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_ExistingApolloFederationSupportWithExtraProperties_WritesOnlyVersion()
    {
        // arrange
        // the settings reader rejects an 'apolloFederationSupport' that carries anything but
        // 'version', so a stale extra property must not survive.
        SetupNoAuthentication();
        SetupFile(
            "schema-settings.json",
            """
            {
              "name": "products",
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": { "version": "2.0", "stale": true }
                }
              }
            }
            """);
        var capturedStream = SetupCreateFile("schema-settings.json");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--kind",
            "apollo-federation");

        // assert
        Assert.Equal(0, result.ExitCode);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "2.0"
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Init_ExistingSettingsWithoutName_ReturnsError()
    {
        // arrange
        SetupNoAuthentication();
        SetupFile("schema-settings.json", """{ "name": "" }""");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--url",
            "https://products.example.com/graphql");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Source schema settings file '/some/working/directory/schema-settings.json' must specify a non-empty string 'name'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Init_ExistingSettingsThatAreNotAnObject_ReturnsError()
    {
        // arrange
        SetupNoAuthentication();
        SetupFile("schema-settings.json", "[]");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Source schema settings file '/some/working/directory/schema-settings.json' does not contain a JSON object. Remove it or write to a different path with '--settings-file'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Init_MissingNameInNonInteractiveMode_ReturnsError()
    {
        // arrange
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing required option '--name'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Init_MissingUrlInNonInteractiveMode_ReturnsError()
    {
        // arrange
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing required option '--url'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Init_InvalidPromptedUrl_ReturnsError()
    {
        // arrange
        SetupNoAuthentication();
        SetupInteractionMode(InteractionMode.Interactive);
        var command = StartInteractiveCommand(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products");

        // act
        command.Input("not-a-url");

        var result = await command.RunToCompletionAsync(TestContext.Current.CancellationToken);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The value for '--url' must be an absolute HTTP URL without user information or a fragment, or reference an environment variable such as '{{API_URL}}'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Init_InvalidUrl_ReturnsError()
    {
        // arrange
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products",
            "--url",
            "not-a-url");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The value for '--url' must be an absolute HTTP URL without user information or a fragment, or reference an environment variable such as '{{API_URL}}'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Init_ApolloFederationVersionWithoutMatchingKind_ReturnsError()
    {
        // arrange
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "source-schema",
            "init",
            "--name",
            "products",
            "--apollo-federation-version",
            "2.0");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The option '--apollo-federation-version' requires '--kind apollo-federation'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Init_Interactive_PromptsForNameAndUrl()
    {
        // arrange
        SetupNoAuthentication();
        SetupInteractionMode(InteractionMode.Interactive);
        var capturedStream = SetupCreateFile("schema-settings.json");
        SetupDirectory("");
        var command = StartInteractiveCommand(
            "fusion",
            "source-schema",
            "init");

        // act
        command.Input("products");
        command.Input("https://products.example.com/graphql");

        var result = await command.RunToCompletionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, result.ExitCode);

        Encoding.UTF8.GetString(capturedStream.ToArray()).MatchInlineSnapshot(
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "https://products.example.com/graphql"
                }
              }
            }
            """);
    }
}
