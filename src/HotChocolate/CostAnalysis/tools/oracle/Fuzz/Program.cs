using System.Text.Json;

namespace HotChocolate.CostAnalysis.Fuzz;

internal static class Program
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task<int> Main(string[] args)
    {
        try
        {
            var options = Arguments.Parse(args);
            if (options.CompareInput is not null)
            {
                Compare(options.CompareInput, options.OracleResults!, options.FuzzFoundDirectory!);
                return 0;
            }

            var cases = await CaseGenerator.GenerateAsync(options.Seed, options.Count);
            var json = JsonSerializer.Serialize(cases, s_jsonOptions);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(options.Output))!);
            await File.WriteAllTextAsync(options.Output, json + Environment.NewLine);
            Console.WriteLine($"seed={options.Seed} cases={cases.Count} output={options.Output}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Compare(string casesPath, string resultsPath, string fuzzFoundDirectory)
    {
        var cases = JsonSerializer.Deserialize<FuzzCase[]>(File.ReadAllText(casesPath), s_jsonOptions) ?? [];
        var results = File.ReadLines(resultsPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<OracleResult>(line, s_jsonOptions)!)
            .ToDictionary(result => result.Id, StringComparer.Ordinal);

        if (results.Count != cases.Length)
        {
            throw ThrowHelper.InvalidOperation(
                $"Oracle returned {results.Count} results for {cases.Length} cases.");
        }

        foreach (var generated in cases)
        {
            if (!results.TryGetValue(generated.Id, out var oracle))
            {
                throw ThrowHelper.InvalidOperation($"Oracle result is missing '{generated.Id}'.");
            }

            if (!string.Equals(generated.Expected.TypeCostBits, oracle.TypeCostBits, StringComparison.Ordinal)
                || !string.Equals(generated.Expected.FieldCostBits, oracle.FieldCostBits, StringComparison.Ordinal))
            {
                var fixturePath = WriteMinimizedFixture(generated, oracle, fuzzFoundDirectory);
                throw ThrowHelper.InvalidOperation(
                    $"Bit-exact mismatch for {generated.Id}: .NET "
                    + $"{generated.Expected.TypeCost}/{generated.Expected.FieldCost} "
                    + $"({generated.Expected.TypeCostBits}/{generated.Expected.FieldCostBits}), oracle "
                    + $"{oracle.TypeCost}/{oracle.FieldCost} ({oracle.TypeCostBits}/{oracle.FieldCostBits}). "
                    + $"Minimized fixture: {fixturePath}");
            }
        }

        Console.WriteLine($"bit-exact agreement: {cases.Length} cases");
    }

    private static string WriteMinimizedFixture(
        FuzzCase generated,
        OracleResult oracle,
        string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var path = System.IO.Path.Combine(
            outputDirectory,
            $"seed-{generated.Seed}-{generated.Id}.json");
        var fixture = new
        {
            id = $"seed-{generated.Seed}-{generated.Id}",
            source = "fuzz-found",
            generated.Sdl,
            generated.Operation,
            generated.OperationName,
            generated.Variables,
            generated.DefaultListSize,
            backend = "exact-case",
            expected = new { oracle.TypeCost, oracle.FieldCost },
            notes = $"Minimized to one deterministic generated case. Seed {generated.Seed}; source {generated.Source}; case {generated.Id}."
        };
        File.WriteAllText(path, JsonSerializer.Serialize(fixture, s_jsonOptions) + Environment.NewLine);
        return path;
    }

    private sealed record Arguments(
        int Seed,
        int Count,
        string Output,
        string? CompareInput,
        string? OracleResults,
        string? FuzzFoundDirectory)
    {
        public static Arguments Parse(string[] args)
        {
            var seed = 1;
            var count = 200;
            string? output = null;
            string? compareInput = null;
            string? oracleResults = null;
            string? fuzzFound = null;

            for (var index = 0; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--seed":
                        seed = int.Parse(args[++index]);
                        break;
                    case "--cases":
                        count = int.Parse(args[++index]);
                        break;
                    case "--emit-only":
                        output = args[++index];
                        break;
                    case "--compare":
                        compareInput = args[++index];
                        break;
                    case "--oracle-results":
                        oracleResults = args[++index];
                        break;
                    case "--fuzz-found":
                        fuzzFound = args[++index];
                        break;
                    default:
                        throw ThrowHelper.InvalidOperation($"Unknown argument '{args[index]}'.");
                }
            }

            if (compareInput is not null)
            {
                if (oracleResults is null || fuzzFound is null)
                {
                    throw ThrowHelper.InvalidOperation(
                        "--compare requires --oracle-results and --fuzz-found.");
                }

                return new(seed, count, string.Empty, compareInput, oracleResults, fuzzFound);
            }

            if (output is null)
            {
                throw ThrowHelper.InvalidOperation("Specify --emit-only OUTPUT.");
            }

            if (count < 1)
            {
                throw ThrowHelper.InvalidOperation("--cases must be positive.");
            }

            return new(seed, count, output, null, null, null);
        }
    }
}
