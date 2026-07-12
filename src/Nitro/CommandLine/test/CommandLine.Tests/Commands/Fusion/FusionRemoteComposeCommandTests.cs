using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionRemoteComposeCommandTests(NitroCommandFixture fixture)
    : FusionCommandTestBase(fixture), IDisposable
{
    private readonly List<string> _tempFiles = [];

    [Fact]
    public async Task Compose_Should_GetSchema_When_FederationSupportIsAbsent()
    {
        const string sourceSchema = "type Query { remote: String }";
        const string settingsJson =
            """
            {
              "name": "Remote",
              "transports": {
                "http": {
                  "url": "http://runtime.example/graphql",
                  "capabilities": {
                    "batching": {
                      "requestBatching": false,
                      "variableBatching": false
                    }
                  }
                }
              }
            }
            """;
        var archiveFile = CreateTempFile();
        SetupFile("remote/schema-settings.json", settingsJson);
        HttpMethod? requestMethod = null;
        Uri? requestUri = null;
        using var client = CreateClient(request =>
        {
            requestMethod = request.Method;
            requestUri = request.RequestUri;
            return Response(HttpStatusCode.OK, sourceSchema);
        });
        SetupHttpClient(client);

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql/schema.graphql",
            "--source-schema-settings-file",
            "remote/schema-settings.json",
            "--archive",
            archiveFile);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(HttpMethod.Get, requestMethod);
        Assert.Equal(
            new Uri("https://composition.example/graphql/schema.graphql"),
            requestUri);
        using var archive = FusionArchive.Open(archiveFile);
        using var configuration = await archive.TryGetSourceSchemaConfigurationAsync(
            "Remote",
            TestContext.Current.CancellationToken);
        Assert.NotNull(configuration);
        Assert.Equal(settingsJson, configuration.Settings.RootElement.ToString());
        await using var schemaStream = await configuration.OpenReadSchemaAsync(
            TestContext.Current.CancellationToken);
        using var reader = new StreamReader(schemaStream);
        Assert.Equal(sourceSchema, await reader.ReadToEndAsync(
            TestContext.Current.CancellationToken));
        using var gatewayConfiguration = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(gatewayConfiguration);
        gatewayConfiguration.Settings.RootElement.ToString().MatchInlineSnapshot(
            """
            {
              "sourceSchemas": {
                "Remote": {
                  "transports": {
                    "http": {
                      "url": "http://runtime.example/graphql",
                      "capabilities": {
                        "batching": {
                          "requestBatching": false,
                          "variableBatching": false
                        }
                      }
                    }
                  }
                }
              }
            }
            """);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("2.0")]
    public async Task Compose_Should_PostServiceQuery_When_FederationSupportIsExplicit(
        string version)
    {
        var sourceSchema = version == "1.0" ? FederationV1Schema : FederationV2Schema;
        var settingsJson = $$"""
            {
              "name": "Products",
              "transports": {
                "http": {
                  "url": "http://runtime.example/graphql"
                }
              },
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "{{version}}"
                  }
                }
              }
            }
            """;
        var archiveFile = CreateTempFile();
        SetupFile("products/schema-settings.json", settingsJson);
        HttpMethod? requestMethod = null;
        string? requestBody = null;
        using var client = CreateClient(async request =>
        {
            requestMethod = request.Method;
            requestBody = await request.Content!.ReadAsStringAsync(
                TestContext.Current.CancellationToken);
            return ServiceSdlResponse(sourceSchema);
        });
        SetupHttpClient(client);

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql",
            "--source-schema-settings-file",
            "products/schema-settings.json",
            "--archive",
            archiveFile);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(HttpMethod.Post, requestMethod);
        Assert.Equal(
            """{"query":"query FusionServiceSdl { _service { sdl } }"}""",
            requestBody);
        using var archive = FusionArchive.Open(archiveFile);
        using var configuration = await archive.TryGetSourceSchemaConfigurationAsync(
            "Products",
            TestContext.Current.CancellationToken);
        Assert.NotNull(configuration);
        Assert.Equal(version, configuration.Settings.RootElement
            .GetProperty("extensions")
            .GetProperty("chillicream")
            .GetProperty("apolloFederationSupport")
            .GetProperty("version")
            .GetString());
        Assert.Equal(
            "http://runtime.example/graphql",
            configuration.Settings.RootElement
                .GetProperty("transports")
                .GetProperty("http")
                .GetProperty("url")
                .GetString());
    }

    [Fact]
    public async Task Compose_Should_PairRepeatedRemoteInputsByOccurrence_When_MixedWithLocalSchema()
    {
        var archiveFile = CreateTempFile();
        SetupFile("local/schema.graphqls", "type Query { local: String }");
        SetupFile(
            "local/schema-settings.json",
            """{ "name": "Local", "transports": { "http": { "url": "http://local/graphql" } } }""");
        SetupFile(
            "a/schema-settings.json",
            """{ "name": "A", "transports": { "http": { "url": "http://a/graphql" } } }""");
        SetupFile(
            "b/schema-settings.json",
            """{ "name": "B", "transports": { "http": { "url": "http://b/graphql" } } }""");
        using var client = CreateClient(request => request.RequestUri!.AbsolutePath switch
        {
            "/a" => Response(HttpStatusCode.OK, "type Query { a: String }"),
            "/b" => Response(HttpStatusCode.OK, "type Query { b: String }"),
            _ => Response(HttpStatusCode.NotFound, "not found")
        });
        SetupHttpClient(client);

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            "local/schema.graphqls",
            "--source-schema-url",
            "https://composition.example/a",
            "--source-schema-settings-file",
            "a/schema-settings.json",
            "--source-schema-url",
            "https://composition.example/b",
            "--source-schema-settings-file",
            "b/schema-settings.json",
            "--archive",
            archiveFile);

        Assert.Equal(0, result.ExitCode);
        using var archive = FusionArchive.Open(archiveFile);
        Assert.Equal(
            ["A", "B", "Local"],
            await archive.GetSourceSchemaNamesAsync(
                TestContext.Current.CancellationToken));
        (string Name, string Schema, string RuntimeUrl)[] expectedConfigurations =
        [
            ("A", "type Query { a: String }", "http://a/graphql"),
            ("B", "type Query { b: String }", "http://b/graphql"),
            ("Local", "type Query { local: String }", "http://local/graphql")
        ];
        (string Name, string Schema, string RuntimeUrl)[] actualConfigurations =
        [
            await ReadSourceSchemaConfigurationAsync(
                archive,
                "A",
                TestContext.Current.CancellationToken),
            await ReadSourceSchemaConfigurationAsync(
                archive,
                "B",
                TestContext.Current.CancellationToken),
            await ReadSourceSchemaConfigurationAsync(
                archive,
                "Local",
                TestContext.Current.CancellationToken)
        ];
        Assert.Equal(expectedConfigurations, actualConfigurations);
    }

    [Fact]
    public async Task ComposeWatch_Should_RefetchRemoteSchema_When_SettingsChangeWithoutRenaming()
    {
        const string initialSourceSchema = "type Query { initial: String }";
        const string updatedSourceSchema = "type Query { updated: String }";
        const string initialSettings =
            """{ "name": "Remote", "transports": { "http": { "url": "http://runtime/initial" } } }""";
        const string updatedSettings =
            """{ "name": "Remote", "transports": { "http": { "url": "http://runtime/updated" } } }""";
        var directory = CreateTempDirectory();
        var settingsFile = Path.Combine(directory, "schema-settings.json");
        var archiveFile = CreateTempFile();
        await File.WriteAllTextAsync(
            settingsFile,
            initialSettings,
            TestContext.Current.CancellationToken);
        SetupFile(settingsFile, initialSettings);
        var requestCount = 0;
        using var client = CreateClient(_ =>
        {
            var currentRequest = Interlocked.Increment(ref requestCount);
            return Response(
                HttpStatusCode.OK,
                currentRequest == 1 ? initialSourceSchema : updatedSourceSchema);
        });
        SetupHttpClient(client);
        using var cancellationTokenSource = CreateWatchCancellationTokenSource();
        var watchCommand = StartInteractiveCommand(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql/schema.graphql",
            "--source-schema-settings-file",
            settingsFile,
            "--archive",
            archiveFile,
            "--watch");
        var watchTask = watchCommand.RunToCompletionAsync(cancellationTokenSource.Token);
        CommandResult? result = null;

        try
        {
            await WaitForSourceSchemaAsync(
                archiveFile,
                "Remote",
                initialSourceSchema,
                cancellationTokenSource.Token);
            Assert.Equal(1, Volatile.Read(ref requestCount));
            SetupFile(settingsFile, updatedSettings);

            await TriggerFileChangeUntilAsync(
                settingsFile,
                updatedSettings,
                () => Volatile.Read(ref requestCount) >= 2,
                cancellationTokenSource.Token);
            await WaitForSourceSchemaAsync(
                archiveFile,
                "Remote",
                updatedSourceSchema,
                cancellationTokenSource.Token);
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
        }
        finally
        {
            await cancellationTokenSource.CancelAsync();
            result = await watchTask;
        }

        Assert.Equal(0, result.ExitCode);
        Assert.True(Volatile.Read(ref requestCount) >= 2);
        using var archive = FusionArchive.Open(archiveFile);
        using var configuration = await archive.TryGetSourceSchemaConfigurationAsync(
            "Remote",
            TestContext.Current.CancellationToken);
        Assert.NotNull(configuration);
        Assert.Equal(
            "http://runtime/updated",
            configuration.Settings.RootElement
                .GetProperty("transports")
                .GetProperty("http")
                .GetProperty("url")
                .GetString());
    }

    [Fact]
    public async Task ComposeWatch_Should_RefetchRemoteSchemas_When_LocalSchemaChanges()
    {
        const string initialLocalSchema = "type Query { local: String }";
        const string updatedLocalSchema = "type Query { local: String updated: String }";
        const string localSettings =
            """{ "name": "Local", "transports": { "http": { "url": "http://local/graphql" } } }""";
        const string remoteSettings =
            """{ "name": "Remote", "transports": { "http": { "url": "http://remote/graphql" } } }""";
        var directory = CreateTempDirectory();
        var localSchemaFile = Path.Combine(directory, "local.graphqls");
        var localSettingsFile = Path.Combine(directory, "local-settings.json");
        var remoteSettingsFile = Path.Combine(directory, "remote-settings.json");
        var archiveFile = CreateTempFile();
        await File.WriteAllTextAsync(
            localSchemaFile,
            initialLocalSchema,
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            localSettingsFile,
            localSettings,
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            remoteSettingsFile,
            remoteSettings,
            TestContext.Current.CancellationToken);
        SetupFile(localSchemaFile, initialLocalSchema);
        SetupFile(localSettingsFile, localSettings);
        SetupFile(remoteSettingsFile, remoteSettings);
        var requestCount = 0;
        using var client = CreateClient(_ =>
        {
            Interlocked.Increment(ref requestCount);
            return Response(HttpStatusCode.OK, "type Query { remote: String }");
        });
        SetupHttpClient(client);
        using var cancellationTokenSource = CreateWatchCancellationTokenSource();
        var watchCommand = StartInteractiveCommand(
            "fusion",
            "compose",
            "--source-schema-file",
            localSchemaFile,
            "--source-schema-url",
            "https://composition.example/graphql/schema.graphql",
            "--source-schema-settings-file",
            remoteSettingsFile,
            "--archive",
            archiveFile,
            "--watch");
        var watchTask = watchCommand.RunToCompletionAsync(cancellationTokenSource.Token);
        CommandResult? result = null;

        try
        {
            await WaitForSourceSchemaAsync(
                archiveFile,
                "Local",
                initialLocalSchema,
                cancellationTokenSource.Token);
            Assert.Equal(1, Volatile.Read(ref requestCount));
            SetupFile(localSchemaFile, updatedLocalSchema);

            await TriggerFileChangeUntilAsync(
                localSchemaFile,
                updatedLocalSchema,
                () => Volatile.Read(ref requestCount) >= 2,
                cancellationTokenSource.Token);
            await WaitForSourceSchemaAsync(
                archiveFile,
                "Local",
                updatedLocalSchema,
                cancellationTokenSource.Token);
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
        }
        finally
        {
            await cancellationTokenSource.CancelAsync();
            result = await watchTask;
        }

        Assert.Equal(0, result.ExitCode);
        Assert.True(Volatile.Read(ref requestCount) >= 2);
    }

    [Fact]
    public async Task ComposeWatch_Should_RejectRemoteNameChangeBeforeFetching_AndPreserveArchive()
    {
        const string initialSettings =
            """{ "name": "Remote", "transports": { "http": { "url": "http://runtime/graphql" } } }""";
        const string renamedSettings =
            """{ "name": "Renamed", "transports": { "http": { "url": "http://runtime/graphql" } } }""";
        const string sourceSchema = "type Query { remote: String }";
        var directory = CreateTempDirectory();
        var settingsFile = Path.Combine(directory, "schema-settings.json");
        var archiveFile = CreateTempFile();
        await File.WriteAllTextAsync(
            settingsFile,
            initialSettings,
            TestContext.Current.CancellationToken);
        SetupFile(settingsFile, initialSettings);
        var requestCount = 0;
        using var client = CreateClient(_ =>
        {
            Interlocked.Increment(ref requestCount);
            return Response(HttpStatusCode.OK, sourceSchema);
        });
        SetupHttpClient(client);
        using var cancellationTokenSource = CreateWatchCancellationTokenSource();
        var watchCommand = StartInteractiveCommand(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql/schema.graphql",
            "--source-schema-settings-file",
            settingsFile,
            "--archive",
            archiveFile,
            "--watch");
        var watchTask = watchCommand.RunToCompletionAsync(cancellationTokenSource.Token);
        CommandResult? result = null;
        byte[]? before = null;

        try
        {
            await WaitForSourceSchemaAsync(
                archiveFile,
                "Remote",
                sourceSchema,
                cancellationTokenSource.Token);
            Assert.Equal(1, Volatile.Read(ref requestCount));
            before = await File.ReadAllBytesAsync(
                archiveFile,
                cancellationTokenSource.Token);
            SetupFile(settingsFile, renamedSettings);

            await TriggerFileChangesAsync(
                settingsFile,
                renamedSettings,
                cancellationTokenSource.Token);
        }
        finally
        {
            await cancellationTokenSource.CancelAsync();
            result = await watchTask;
        }

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(1, Volatile.Read(ref requestCount));
        Assert.Contains(
            "A source schema settings 'name' cannot change during watch mode.",
            result.StdErr,
            StringComparison.Ordinal);
        Assert.Equal(
            before,
            await File.ReadAllBytesAsync(
                archiveFile,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ComposeWatch_Should_PreserveArchive_When_RemoteRefetchFails()
    {
        const string initialSettings =
            """{ "name": "Remote", "transports": { "http": { "url": "http://runtime/initial" } } }""";
        const string updatedSettings =
            """{ "name": "Remote", "transports": { "http": { "url": "http://runtime/updated" } } }""";
        const string sourceSchema = "type Query { remote: String }";
        var directory = CreateTempDirectory();
        var settingsFile = Path.Combine(directory, "schema-settings.json");
        var archiveFile = CreateTempFile();
        await File.WriteAllTextAsync(
            settingsFile,
            initialSettings,
            TestContext.Current.CancellationToken);
        SetupFile(settingsFile, initialSettings);
        var requestCount = 0;
        using var client = CreateClient(_ =>
        {
            var currentRequest = Interlocked.Increment(ref requestCount);
            return currentRequest == 1
                ? Response(HttpStatusCode.OK, sourceSchema)
                : Response(HttpStatusCode.ServiceUnavailable, "unavailable");
        });
        SetupHttpClient(client);
        using var cancellationTokenSource = CreateWatchCancellationTokenSource();
        var watchCommand = StartInteractiveCommand(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql/schema.graphql",
            "--source-schema-settings-file",
            settingsFile,
            "--archive",
            archiveFile,
            "--watch");
        var watchTask = watchCommand.RunToCompletionAsync(cancellationTokenSource.Token);
        CommandResult? result = null;
        byte[]? before = null;

        try
        {
            await WaitForSourceSchemaAsync(
                archiveFile,
                "Remote",
                sourceSchema,
                cancellationTokenSource.Token);
            Assert.Equal(1, Volatile.Read(ref requestCount));
            before = await File.ReadAllBytesAsync(
                archiveFile,
                cancellationTokenSource.Token);
            SetupFile(settingsFile, updatedSettings);

            await TriggerFileChangeUntilAsync(
                settingsFile,
                updatedSettings,
                () => Volatile.Read(ref requestCount) >= 2,
                cancellationTokenSource.Token);
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
        }
        finally
        {
            await cancellationTokenSource.CancelAsync();
            result = await watchTask;
        }

        Assert.Equal(0, result.ExitCode);
        Assert.True(Volatile.Read(ref requestCount) >= 2);
        Assert.Contains(
            "Source schema 'Remote' returned HTTP 503 (Service Unavailable) while downloading its schema.",
            result.StdErr,
            StringComparison.Ordinal);
        Assert.Equal(
            before,
            await File.ReadAllBytesAsync(
                archiveFile,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Compose_Should_LeaveArchiveUnchanged_When_RemoteVersionIsUnsupported()
    {
        var archiveFile = CreateTempFile();
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "__resources__", "fusion-archives", "gateway.far"),
            archiveFile);
        SetupFile(archiveFile, new MemoryStream(File.ReadAllBytes(archiveFile)));
        var before = await File.ReadAllBytesAsync(
            archiveFile,
            TestContext.Current.CancellationToken);
        SetupFile(
            "remote/schema-settings.json",
            """
            {
              "name": "Remote",
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": "3.0"
                  }
                }
              }
            }
            """);
        var requestCount = 0;
        using var client = CreateClient(_ =>
        {
            requestCount++;
            return Response(HttpStatusCode.OK, "type Query { remote: String }");
        });
        SetupHttpClient(client);

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql",
            "--source-schema-settings-file",
            "remote/schema-settings.json",
            "--archive",
            archiveFile);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(0, requestCount);
        Assert.Equal(
            before,
            await File.ReadAllBytesAsync(
                archiveFile,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Compose_Should_NotCreateArchive_When_RemoteFetchFails()
    {
        var archiveFile = CreateTempFile();
        SetupFile(
            "remote/schema-settings.json",
            """{ "name": "Remote", "transports": { "http": { "url": "http://runtime/graphql" } } }""");
        using var client = CreateClient(
            _ => Response(HttpStatusCode.ServiceUnavailable, "unavailable"));
        SetupHttpClient(client);

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql/schema.graphql",
            "--source-schema-settings-file",
            "remote/schema-settings.json",
            "--archive",
            archiveFile);

        Assert.Equal(1, result.ExitCode);
        Assert.False(File.Exists(archiveFile));
    }

    [Fact]
    public async Task Compose_Should_LeaveArchiveUnchanged_When_RemoteFetchFails()
    {
        var archiveFile = CreateTempFile();
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "__resources__", "fusion-archives", "gateway.far"),
            archiveFile);
        SetupFile(archiveFile, new MemoryStream(File.ReadAllBytes(archiveFile)));
        var before = await File.ReadAllBytesAsync(
            archiveFile,
            TestContext.Current.CancellationToken);
        SetupFile(
            "remote/schema-settings.json",
            """{ "name": "Remote", "transports": { "http": { "url": "http://runtime/graphql" } } }""");
        using var client = CreateClient(
            _ => Response(HttpStatusCode.ServiceUnavailable, "unavailable"));
        SetupHttpClient(client);

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql/schema.graphql",
            "--source-schema-settings-file",
            "remote/schema-settings.json",
            "--archive",
            archiveFile);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            before,
            await File.ReadAllBytesAsync(
                archiveFile,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Compose_Should_RejectOversizedGetResponse()
    {
        var archiveFile = CreateTempFile();
        SetupFile(
            "remote/schema-settings.json",
            """{ "name": "Remote", "transports": { "http": { "url": "http://runtime/graphql" } } }""");
        using var client = CreateClient(_ =>
        {
            var response = Response(HttpStatusCode.OK, "type Query { remote: String }");
            response.Content.Headers.ContentLength = SchemaHttpResponseReader.MaxResponseSize + 1;
            return response;
        });
        SetupHttpClient(client);

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql/schema.graphql?token=secret",
            "--source-schema-settings-file",
            "remote/schema-settings.json",
            "--archive",
            archiveFile);

        Assert.Equal(1, result.ExitCode);
        result.AssertError(
            "Source schema 'Remote' returned a response larger than the maximum allowed size of 50000000 bytes.");
        Assert.False(File.Exists(archiveFile));
    }

    [Fact]
    public async Task Compose_Should_SanitizeGetResponseReadFailure()
    {
        var archiveFile = CreateTempFile();
        SetupFile(
            "remote/schema-settings.json",
            """{ "name": "Remote", "transports": { "http": { "url": "http://runtime/graphql" } } }""");
        using var client = CreateClient(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new FailingReadStream())
            });
        SetupHttpClient(client);

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql?token=secret",
            "--source-schema-settings-file",
            "remote/schema-settings.json",
            "--archive",
            archiveFile);

        Assert.Equal(1, result.ExitCode);
        result.AssertError(
            "Failed to connect to source schema 'Remote' while downloading its schema.");
        Assert.False(File.Exists(archiveFile));
    }

    [Fact]
    public async Task Compose_Should_NotSendRequests_When_LocalAndRemoteNamesAreDuplicated()
    {
        var archiveFile = CreateTempFile();
        SetupFile("local/schema.graphqls", "type Query { local: String }");
        SetupFile(
            "local/schema-settings.json",
            """{ "name": "Duplicate", "transports": { "http": { "url": "http://local/graphql" } } }""");
        SetupFile(
            "remote/schema-settings.json",
            """{ "name": "Duplicate", "transports": { "http": { "url": "http://remote/graphql" } } }""");
        var requestCount = 0;
        using var client = CreateClient(_ =>
        {
            requestCount++;
            return Response(HttpStatusCode.OK, "type Query { remote: String }");
        });
        SetupHttpClient(client);

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-file",
            "local/schema.graphqls",
            "--source-schema-url",
            "https://composition.example/graphql",
            "--source-schema-settings-file",
            "remote/schema-settings.json",
            "--archive",
            archiveFile);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(0, requestCount);
        result.AssertError("Source schema 'Duplicate' was specified more than once.");
    }

    [Fact]
    public async Task Compose_Should_NotSendRequests_When_RemoteNamesAreDuplicated()
    {
        var archiveFile = CreateTempFile();
        const string settings =
            """{ "name": "Duplicate", "transports": { "http": { "url": "http://runtime/graphql" } } }""";
        SetupFile("a/schema-settings.json", settings);
        SetupFile("b/schema-settings.json", settings);
        var requestCount = 0;
        using var client = CreateClient(_ =>
        {
            requestCount++;
            return Response(HttpStatusCode.OK, "type Query { remote: String }");
        });
        SetupHttpClient(client);

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/a",
            "--source-schema-settings-file",
            "a/schema-settings.json",
            "--source-schema-url",
            "https://composition.example/b",
            "--source-schema-settings-file",
            "b/schema-settings.json",
            "--archive",
            archiveFile);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(0, requestCount);
        result.AssertError("Source schema 'Duplicate' was specified more than once.");
    }

    [Fact]
    public async Task Compose_Should_NotSendRequest_When_FederationSupportShapeIsInvalid()
    {
        var archiveFile = CreateTempFile();
        SetupFile(
            "remote/schema-settings.json",
            """
            {
              "name": "Remote",
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": {
                    "version": true
                  }
                }
              }
            }
            """);
        var requestCount = 0;
        using var client = CreateClient(_ =>
        {
            requestCount++;
            return Response(HttpStatusCode.OK, "type Query { remote: String }");
        });
        SetupHttpClient(client);

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql",
            "--source-schema-settings-file",
            "remote/schema-settings.json",
            "--archive",
            archiveFile);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(0, requestCount);
        Assert.False(File.Exists(archiveFile));
    }

    [Theory]
    [InlineData("relative")]
    [InlineData("https://user:secret@composition.example/graphql?token=secret")]
    [InlineData("https://composition.example/graphql#secret")]
    public async Task Compose_Should_RejectUnsafeSourceSchemaUrl_WithoutEchoingIt(
        string sourceSchemaUrl)
    {
        var archiveFile = CreateTempFile();

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-url",
            sourceSchemaUrl,
            "--source-schema-settings-file",
            "remote/schema-settings.json",
            "--archive",
            archiveFile);

        result.AssertError(
            "The value for '--source-schema-url' must be an absolute HTTP URL without user information or a fragment.");
        Assert.False(File.Exists(archiveFile));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    [InlineData(1, 2)]
    public async Task Compose_Should_RejectUnequalRemoteInputCounts_BeforeFetchingOrCreatingArchive(
        int sourceSchemaUrlCount,
        int sourceSchemaSettingsFileCount)
    {
        var archiveFile = CreateTempFile();
        List<string> arguments = ["fusion", "compose"];

        for (var i = 0; i < sourceSchemaUrlCount; i++)
        {
            arguments.AddRange(
                ["--source-schema-url", $"https://composition.example/schema-{i}"]);
        }

        for (var i = 0; i < sourceSchemaSettingsFileCount; i++)
        {
            var settingsFile = $"remote-{i}/schema-settings.json";
            SetupFile(
                settingsFile,
                $$"""{ "name": "Remote{{i}}" }""");
            arguments.AddRange(["--source-schema-settings-file", settingsFile]);
        }

        arguments.AddRange(["--archive", archiveFile]);
        var requestCount = 0;
        using var client = CreateClient(_ =>
        {
            Interlocked.Increment(ref requestCount);
            return Response(HttpStatusCode.OK, "type Query { remote: String }");
        });
        SetupHttpClient(client);

        var result = await ExecuteCommandAsync([.. arguments]);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(0, requestCount);
        Assert.False(File.Exists(archiveFile));
    }

    private string CreateTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _tempFiles.Add(path);
        return path;
    }

    private string CreateTempDirectory()
    {
        var path = CreateTempFile();
        Directory.CreateDirectory(path);
        return path;
    }

    private static CancellationTokenSource CreateWatchCancellationTokenSource()
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(15));
        return cancellationTokenSource;
    }

    private static async Task TriggerFileChangeUntilAsync(
        string file,
        string content,
        Func<bool> condition,
        CancellationToken cancellationToken)
    {
        var suffixLength = 1;

        while (!condition())
        {
            await File.WriteAllTextAsync(
                file,
                content + new string(' ', suffixLength++),
                cancellationToken);

            for (var i = 0; i < 20 && !condition(); i++)
            {
                await Task.Delay(50, cancellationToken);
            }
        }
    }

    private static async Task TriggerFileChangesAsync(
        string file,
        string content,
        CancellationToken cancellationToken)
    {
        for (var i = 1; i <= 3; i++)
        {
            await File.WriteAllTextAsync(
                file,
                content + new string(' ', i),
                cancellationToken);
            await Task.Delay(500, cancellationToken);
        }
    }

    private static async Task WaitForSourceSchemaAsync(
        string archiveFile,
        string sourceSchemaName,
        string expectedSourceSchema,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(archiveFile))
            {
                try
                {
                    using var archive = FusionArchive.Open(archiveFile);
                    using var configuration = await archive.TryGetSourceSchemaConfigurationAsync(
                        sourceSchemaName,
                        cancellationToken);

                    if (configuration is not null)
                    {
                        await using var schemaStream = await configuration.OpenReadSchemaAsync(
                            cancellationToken);
                        using var reader = new StreamReader(schemaStream);

                        if (await reader.ReadToEndAsync(cancellationToken) == expectedSourceSchema)
                        {
                            return;
                        }
                    }
                }
                catch (IOException)
                {
                    // The watch composition is still committing the archive.
                }
                catch (InvalidDataException)
                {
                    // The watch composition is still committing the archive.
                }
            }

            await Task.Delay(50, cancellationToken);
        }
    }

    private static async Task<(string Name, string Schema, string RuntimeUrl)>
        ReadSourceSchemaConfigurationAsync(
            FusionArchive archive,
            string sourceSchemaName,
            CancellationToken cancellationToken)
    {
        using var configuration = await archive.TryGetSourceSchemaConfigurationAsync(
            sourceSchemaName,
            cancellationToken);
        Assert.NotNull(configuration);
        await using var schemaStream = await configuration.OpenReadSchemaAsync(cancellationToken);
        using var reader = new StreamReader(schemaStream);
        var schema = await reader.ReadToEndAsync(cancellationToken);
        var runtimeUrl = configuration.Settings.RootElement
            .GetProperty("transports")
            .GetProperty("http")
            .GetProperty("url")
            .GetString();
        Assert.NotNull(runtimeUrl);
        return (sourceSchemaName, schema, runtimeUrl);
    }

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        => CreateClient(request => Task.FromResult(responseFactory(request)));

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        => new(new StubHttpMessageHandler(responseFactory));

    private static HttpResponseMessage Response(HttpStatusCode statusCode, string content)
        => new(statusCode) { Content = new StringContent(content) };

    private static HttpResponseMessage ServiceSdlResponse(string sourceSchema)
        => Response(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new
            {
                data = new
                {
                    _service = new { sdl = sourceSchema }
                }
            }));

    public void Dispose()
    {
        foreach (var path in _tempFiles)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => responseFactory(request);
    }

    private sealed class FailingReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
            => throw new IOException("https://composition.example/graphql?token=secret");

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
            => ValueTask.FromException<int>(
                new IOException("https://composition.example/graphql?token=secret"));

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();
    }

    private const string FederationV1Schema =
        """
        scalar _Any
        scalar _FieldSet

        type _Service {
          sdl: String
        }

        union _Entity = Product

        type Query {
          product: Product
          _entities(representations: [_Any!]!): [_Entity]!
          _service: _Service!
        }

        type Product @key(fields: "id") {
          id: ID!
          name: String
        }
        """;

    private const string FederationV2Schema =
        """
        extend schema
          @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"])

        type Query {
          product: Product
        }

        type Product @key(fields: "id") {
          id: ID!
          name: String
        }
        """;
}
