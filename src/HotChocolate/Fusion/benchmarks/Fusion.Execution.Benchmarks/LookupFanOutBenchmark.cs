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
/// Executes the federation benchmark query (users and topProducts with nested reviews, products
/// and authors) through the gateway executor against canned subgraph responses, so the measured
/// work is planning lookup, variable creation, response parsing, result composition and
/// serialization without transport. Waves 3 and 4 of this query fan one lookup result out to
/// several hundred result positions.
/// </summary>
[MemoryDiagnoser]
public class LookupFanOutBenchmark
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
    private IRequestExecutor _executor = null!;
    private IOperationRequest _request = null!;

    [Params("composite-schema", "apollo-federation")]
    public string Family { get; set; } = "composite-schema";

    [GlobalSetup]
    public async Task SetupAsync()
    {
        var schemaDocument = Utf8GraphQLParser.Parse(ReadSchema(Family));
        string[] schemaNames = Family == "composite-schema"
            ? ["accounts-api", "inventory-api", "products-api", "reviews-api"]
            : ["accounts", "inventory", "products", "reviews"];

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

        _executor = await services.BuildGatewayAsync();
        _request = OperationRequestBuilder.New().SetDocument(Query).Build();

        var length = await ExecuteAsync();

        if (length < 80_000)
        {
            _output.Reset();
            await using var result = await _executor.ExecuteAsync(_request);
            throw new InvalidOperationException(
                $"Unexpected response size {length}: {result.ToJson()}");
        }

        // The digest makes the responses of two builds comparable from the benchmark log.
        var response = new byte[length];
        _output.CopyTo(response, 0, length);
        Console.WriteLine(
            $"// {Family} response: {length} bytes, sha256 {Convert.ToHexStringLower(SHA256.HashData(response))}");
    }

    [Benchmark]
    public Task<int> ExecuteQuery() => ExecuteAsync();

    private async Task<int> ExecuteAsync()
    {
        _output.Reset();

        await using var result = await _executor.ExecuteAsync(_request);
        JsonResultFormatter.Default.Format(result.ExpectOperationResult(), _output);
        return _output.Length;
    }

    private static string ReadSchema(string family)
    {
        var assembly = typeof(LookupFanOutBenchmark).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .Single(t => t.EndsWith(family + ".graphqls", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
