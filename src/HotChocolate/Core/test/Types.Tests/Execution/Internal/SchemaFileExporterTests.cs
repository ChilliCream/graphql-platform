using HotChocolate.Types;
using HotChocolate.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Internal;

public class SchemaFileExporterTests : IDisposable
{
    private readonly string _directory = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        System.IO.Path.GetRandomFileName());

    [Fact]
    public async Task Export_Should_DeclareBothBatchingModes_When_NoProviderIsRegistered()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"));
        var executor = await GetExecutorAsync(services);

        // act
        var result = await SchemaFileExporter.Export(
            System.IO.Path.Combine(_directory, "schema.graphqls"),
            executor,
            rewriteToSemanticNonNull: false,
            specVersion: null,
            TestContext.Current.CancellationToken);

        // assert
        var settings = await File.ReadAllTextAsync(
            result.SettingsFileName,
            TestContext.Current.CancellationToken);
        settings.ReplaceLineEndings("\n").MatchInlineSnapshot(
            """
            {
              "name": "_Default",
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
            """ + "\n");
    }

    [Fact]
    public async Task Export_Should_DeclareProviderCapabilities_When_ProviderIsRegistered()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITransportCapabilitiesProvider>(
            new FixedCapabilitiesProvider(
                new TransportCapabilities(VariableBatching: false, RequestBatching: true)));
        services
            .AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"));
        var executor = await GetExecutorAsync(services);

        // act
        var result = await SchemaFileExporter.Export(
            System.IO.Path.Combine(_directory, "schema.graphqls"),
            executor,
            rewriteToSemanticNonNull: false,
            specVersion: null,
            TestContext.Current.CancellationToken);

        // assert
        var settings = await File.ReadAllTextAsync(
            result.SettingsFileName,
            TestContext.Current.CancellationToken);
        settings.ReplaceLineEndings("\n").MatchInlineSnapshot(
            """
            {
              "name": "_Default",
              "transports": {
                "http": {
                  "url": "http://localhost:5000/graphql",
                  "capabilities": {
                    "batching": {
                      "variableBatching": false,
                      "requestBatching": true,
                      "aliasBatching": true
                    },
                    "onError": "propagate"
                  }
                }
              }
            }
            """ + "\n");
    }

    [Fact]
    public async Task Export_Should_CreateDirectory_When_DirectoryDoesNotExist()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"));
        var executor = await GetExecutorAsync(services);

        // act
        var result = await SchemaFileExporter.Export(
            System.IO.Path.Combine(_directory, "nested", "schema.graphqls"),
            executor,
            rewriteToSemanticNonNull: false,
            specVersion: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(File.Exists(result.SchemaFileName));
        Assert.True(File.Exists(result.SettingsFileName));
    }

    [Fact]
    public async Task Export_Should_DowngradeSchema_When_October2021SpecVersionIsSpecified()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name("Query").Tag("schema-export");
                d.Field("foo").Resolve("bar");
            });
        var executor = await GetExecutorAsync(services);

        // act
        var result = await SchemaFileExporter.Export(
            System.IO.Path.Combine(_directory, "schema.graphqls"),
            executor,
            rewriteToSemanticNonNull: false,
            GraphQLSpecVersion.October2021,
            TestContext.Current.CancellationToken);

        // assert
        var schema = await File.ReadAllTextAsync(result.SchemaFileName, TestContext.Current.CancellationToken);
        schema.ReplaceLineEndings("\n").MatchInlineSnapshot(
            """"
            schema {
              query: Query
            }

            type Query @tag(name: "schema-export") {
              foo: String
            }

            """
            The @tag directive is used to apply arbitrary string
            metadata to a schema location. Custom tooling can use
            this metadata during any step of the schema delivery flow,
            including composition, static analysis, and documentation.

            interface Book {
              id: ID! @tag(name: "your-value")
              title: String!
              author: String!
            }
            """
            directive @tag("The name of the tag." name: String!) repeatable on
              | SCHEMA
              | SCALAR
              | OBJECT
              | FIELD_DEFINITION
              | ARGUMENT_DEFINITION
              | INTERFACE
              | UNION
              | ENUM
              | ENUM_VALUE
              | INPUT_OBJECT
              | INPUT_FIELD_DEFINITION
            """" + "\n");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static async Task<IRequestExecutor> GetExecutorAsync(IServiceCollection services)
        => await services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    private sealed class FixedCapabilitiesProvider(TransportCapabilities capabilities)
        : ITransportCapabilitiesProvider
    {
        public TransportCapabilities GetCapabilities(string schemaName) => capabilities;
    }
}
