using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Benchmarks.LookupFanOut;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Language;
using HotChocolate.Transport.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Serializes the composite result of the <see cref="LookupFanOutBenchmark"/> query for the
/// composite schema family to JSON, so the measured work is only the result formatter.
/// </summary>
[MemoryDiagnoser]
public class LookupFanOutFormatBenchmark
{
    private const string Query =
        """
        fragment User on User { id username name }
        fragment Review on Review { id body }
        fragment Product on Product { inStock name price shippingEstimate upc weight }

        query TestQuery {
          users { ...User reviews { ...Review product { ...Product reviews { ...Review author { ...User reviews { ...Review product { ...Product } } } } } } }
          topProducts { ...Product reviews { ...Review author { ...User reviews { ...Review product { ...Product } } } } }
        }
        """;

    private readonly ChunkedArrayWriter _output = new();
    private IExecutionResult _result = null!;
    private OperationResult _operationResult = null!;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        var executor = await CreateExecutorAsync();
        var request = OperationRequestBuilder.New().SetDocument(Query).Build();
        _result = await executor.ExecuteAsync(request);
        _operationResult = _result.ExpectOperationResult();

        var length = Format();

        if (length < 80_000)
        {
            throw new InvalidOperationException($"Unexpected response size {length}.");
        }

        // The digest makes the responses of two builds comparable from the benchmark log.
        var response = new byte[length];
        _output.CopyTo(response, 0, length);
        Console.WriteLine(
            $"// format response: {length} bytes, sha256 {Convert.ToHexStringLower(SHA256.HashData(response))}");
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        try
        {
            await _result.DisposeAsync();
        }
        finally
        {
            _output.Dispose();
        }
    }

    [Benchmark]
    public int FormatResult() => Format();

    private int Format()
    {
        _output.Reset();
        JsonResultFormatter.Default.Format(_operationResult, _output);
        return _output.Length;
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync()
    {
        var schemaDocument = Utf8GraphQLParser.Parse(ReadSchema());
        string[] schemaNames = ["accounts-api", "inventory-api", "products-api", "reviews-api"];

        var services = new ServiceCollection();
        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(schemaDocument)
            .ModifyCostOptions(o => o.EnforceCostLimits = false);

        services.RemoveAll<ISourceSchemaClientFactory>();
        services.AddSingleton<ISourceSchemaClientFactory, CannedSourceSchemaClientFactory>();

        foreach (var schemaName in schemaNames)
        {
            FusionSetupUtilities.Configure(
                builder,
                setup => setup.ClientConfigurationModifiers.Add(
                    _ => new CannedSourceSchemaClientConfiguration(schemaName)));
        }

        return await services.BuildGatewayAsync();
    }

    private static string ReadSchema()
    {
        var assembly = typeof(LookupFanOutFormatBenchmark).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .Single(t => t.EndsWith("composite-schema.graphqls", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
