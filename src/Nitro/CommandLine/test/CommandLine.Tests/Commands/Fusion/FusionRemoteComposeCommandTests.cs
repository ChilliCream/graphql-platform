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
    public async Task Compose_Should_MixLocalAndRepeatedRemoteSourceSchemas()
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
            "a/schema-settings.json",
            "--source-schema-url",
            "https://composition.example/b",
            "b/schema-settings.json",
            "--archive",
            archiveFile);

        Assert.Equal(0, result.ExitCode);
        using var archive = FusionArchive.Open(archiveFile);
        Assert.Equal(
            ["A", "B", "Local"],
            await archive.GetSourceSchemaNamesAsync(
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
            "a/schema-settings.json",
            "--source-schema-url",
            "https://composition.example/b",
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
            "remote/schema-settings.json",
            "--archive",
            archiveFile);

        result.AssertError(
            "The value for '--source-schema-url' must be an absolute HTTP URL without user information or a fragment.");
        Assert.False(File.Exists(archiveFile));
    }

    [Fact]
    public async Task Compose_Should_RejectSourceSchemaUrlWithoutSettingsFile()
    {
        var archiveFile = CreateTempFile();

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql",
            "--archive",
            archiveFile);

        Assert.Equal(1, result.ExitCode);
        Assert.False(File.Exists(archiveFile));
    }

    [Fact]
    public async Task Compose_Should_RejectThreeValuesForOneSourceSchemaUrlOccurrence()
    {
        var archiveFile = CreateTempFile();

        var result = await ExecuteCommandAsync(
            "fusion",
            "compose",
            "--source-schema-url",
            "https://composition.example/graphql",
            "remote/schema-settings.json",
            "unexpected",
            "--archive",
            archiveFile);

        Assert.Equal(1, result.ExitCode);
        Assert.False(File.Exists(archiveFile));
    }

    private string CreateTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _tempFiles.Add(path);
        return path;
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
            if (File.Exists(path))
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
