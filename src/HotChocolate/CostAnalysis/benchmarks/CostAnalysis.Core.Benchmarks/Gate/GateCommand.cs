using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HotChocolate.CostAnalysis;

internal static partial class GateCommand
{
    private static readonly Endpoint[] s_expectedEndpoints =
    [
        new(1_024, 80, 4, 8, 2, 2),
        new(1_024, 80, 4, 80, 2, 2),
        new(10_240, 80, 4, 8, 2, 2)
    ];

    private static readonly HashSet<string> s_generatedArtifactPaths =
    [
        "src/HotChocolate/CostAnalysis/benchmarks/results/head-to-head/cost-endpoints.csv",
        "src/HotChocolate/CostAnalysis/benchmarks/results/head-to-head/cost-query-size.csv",
        "src/HotChocolate/CostAnalysis/benchmarks/results/head-to-head/cost-schema-size.csv",
        "src/HotChocolate/CostAnalysis/benchmarks/results/head-to-head/oracle-cost-endpoints.csv",
        "src/HotChocolate/CostAnalysis/benchmarks/results/head-to-head/pathological-booleans.csv",
        "src/HotChocolate/CostAnalysis/benchmarks/results/head-to-head/provenance.json",
        "src/HotChocolate/CostAnalysis/benchmarks/results/micro/HotChocolate.CostAnalysis.AdversarialCorrelatedBooleansBenchmark-report.csv",
        "src/HotChocolate/CostAnalysis/benchmarks/results/micro/HotChocolate.CostAnalysis.CostPlanBenchmark-report.csv",
        "src/HotChocolate/CostAnalysis/benchmarks/results/micro/HotChocolate.CostAnalysis.LargeSchemaSnapshotBenchmark-report.csv"
    ];

    private const string MicroDirectory =
        "src/HotChocolate/CostAnalysis/benchmarks/results/micro";
    private const string CostPlanReport =
        "HotChocolate.CostAnalysis.CostPlanBenchmark-report.csv";
    private const string AdversarialReport =
        "HotChocolate.CostAnalysis.AdversarialCorrelatedBooleansBenchmark-report.csv";
    private const string HeadToHeadReport =
        "src/HotChocolate/CostAnalysis/benchmarks/results/head-to-head/cost-endpoints.csv";
    private const string HeadToHeadProvenanceFile =
        "src/HotChocolate/CostAnalysis/benchmarks/results/head-to-head/provenance.json";
    private const string ConfigurationFile =
        "src/HotChocolate/CostAnalysis/benchmarks/results/gates.json";
    private const string EngineOptionsFile =
        "src/HotChocolate/CostAnalysis/src/CostAnalysis.Core/CostEngineOptions.cs";
    private const string OracleRevision = "fec57fd7a980b5399637fa464a9bdce0781d91dd";
    private const int CampaignSeed = 20260829;
    private const int CampaignReplicates = 10;

    public static int Run()
    {
        try
        {
            var repositoryRoot = FindRepositoryRoot();
            var configuration = ReadConfiguration(Path.Combine(repositoryRoot, ConfigurationFile));
            ValidateConfiguration(configuration);
            ValidateEngineDefault(repositoryRoot, configuration);
            ValidateHeadToHeadProvenance(repositoryRoot, configuration);

            var failures = new List<string>();
            CheckWarmGate(repositoryRoot, configuration, failures);
            CheckAdversarialGate(repositoryRoot, configuration, failures);
            CheckColdGate(repositoryRoot, configuration, failures);

            if (failures.Count == 0)
            {
                Console.WriteLine($"Cost performance gates passed (frozen at {configuration.FrozenAt}).");
                return 0;
            }

            foreach (var failure in failures)
            {
                Console.Error.WriteLine($"FAIL {failure}");
            }

            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Gate input error: {exception.Message}");
            return 2;
        }
    }

    private static void CheckWarmGate(
        string repositoryRoot,
        GateConfiguration configuration,
        List<string> failures)
    {
        var path = Path.Combine(repositoryRoot, MicroDirectory, CostPlanReport);
        var table = CsvTable.Read(path);
        table.RequireColumn("Method");
        table.RequireColumn("Median");
        table.RequireColumn("Allocated");
        var row = SingleRow(table, path, "Method", "WarmEvaluate");
        var p50Us = ParseDurationMicroseconds(path, "Median", row["Median"]);
        var allocatedBytes = ParseBytes(path, "Allocated", row["Allocated"]);

        CheckMaximum(
            "warm evaluate p50",
            p50Us,
            configuration.WarmEvaluateP50Us,
            "us",
            failures);
        CheckExact(
            "warm evaluate allocation",
            allocatedBytes,
            configuration.WarmAllocatedBytes,
            failures);
    }

    private static void CheckAdversarialGate(
        string repositoryRoot,
        GateConfiguration configuration,
        List<string> failures)
    {
        var path = Path.Combine(repositoryRoot, MicroDirectory, AdversarialReport);
        var table = CsvTable.Read(path);
        table.RequireColumn("Method");
        table.RequireColumn("VariableCount");
        table.RequireColumn("Median");

        var exactVariableCount = GetFullyCoveredVariableCount(configuration.CaseBudget);
        var fallbackVariableCount = exactVariableCount + 1;
        var exactMilliseconds = ReadAdversarialMilliseconds(
            table,
            path,
            exactVariableCount);
        var fallbackMilliseconds = ReadAdversarialMilliseconds(
            table,
            path,
            fallbackVariableCount);

        CheckMaximum(
            $"adversarial exact-boundary p50 ({exactVariableCount} variables, {configuration.CaseBudget} splits)",
            exactMilliseconds,
            configuration.AdversarialMs,
            "ms",
            failures);
        CheckMaximum(
            $"adversarial fallback p50 ({fallbackVariableCount} variables, {configuration.CaseBudget} splits)",
            fallbackMilliseconds,
            configuration.AdversarialMs,
            "ms",
            failures);
    }

    private static double ReadAdversarialMilliseconds(
        CsvTable table,
        string path,
        int variableCount)
    {
        var row = table.Rows.SingleOrDefault(
            row => row["Method"].Equals(
                    "AdversarialCorrelatedBooleans",
                    StringComparison.Ordinal)
                && ParseInteger(path, "VariableCount", row["VariableCount"]) == variableCount)
            ?? throw GateThrowHelper.RowNotFound(
                path,
                $"the {variableCount}-variable adversarial case");
        return ParseDurationMicroseconds(path, "Median", row["Median"]) / 1_000.0;
    }

    private static void CheckColdGate(
        string repositoryRoot,
        GateConfiguration configuration,
        List<string> failures)
    {
        var path = Path.Combine(repositoryRoot, HeadToHeadReport);
        var table = CsvTable.Read(path);
        table.RequireColumn("backend");
        table.RequireColumn("object_types");
        table.RequireColumn("abstract_types");
        table.RequireColumn("incidences_per_object");
        table.RequireColumn("query_spreads");
        table.RequireColumn("type_cost");
        table.RequireColumn("field_cost");
        table.RequireColumn("median_ns_per_op");

        var groups = table.Rows
            .Where(row => row["backend"] is "hotchocolate-cold" or "rust-exact-case")
            .GroupBy(row => new Endpoint(
                ParseInteger(path, "object_types", row["object_types"]),
                ParseInteger(path, "abstract_types", row["abstract_types"]),
                ParseInteger(path, "incidences_per_object", row["incidences_per_object"]),
                ParseInteger(path, "query_spreads", row["query_spreads"]),
                ParseInteger(path, "type_cost", row["type_cost"]),
                ParseInteger(path, "field_cost", row["field_cost"])))
            .OrderBy(group => group.Key.ObjectTypes)
            .ThenBy(group => group.Key.QuerySpreads)
            .ToArray();

        if (groups.Length != s_expectedEndpoints.Length
            || !groups.Select(group => group.Key).SequenceEqual(s_expectedEndpoints))
        {
            throw GateThrowHelper.UnexpectedEndpoints(path);
        }

        foreach (var group in groups)
        {
            var hotChocolate = MedianForBackend(group, path, "hotchocolate-cold");
            var rust = MedianForBackend(group, path, "rust-exact-case");
            var ratio = hotChocolate / rust;
            CheckMaximum(
                $"cold {group.Key.ObjectTypes} objects/{group.Key.QuerySpreads} spreads vs Rust",
                ratio,
                configuration.ColdRustBand,
                "x",
                failures);
        }
    }

    private static CsvRow SingleRow(
        CsvTable table,
        string path,
        string column,
        string value)
        => table.Rows.SingleOrDefault(
            row => row[column].Equals(value, StringComparison.Ordinal))
            ?? throw GateThrowHelper.RowNotFound(path, $"the '{value}' row");

    private static double MedianForBackend(
        IEnumerable<CsvRow> rows,
        string path,
        string backend)
    {
        var values = rows
            .Where(row => row["backend"].Equals(backend, StringComparison.Ordinal))
            .Select(row => ParsePositiveDouble(
                path,
                "median_ns_per_op",
                row["median_ns_per_op"]))
            .Order()
            .ToArray();

        if (values.Length != 10)
        {
            throw GateThrowHelper.UnexpectedRowCount(path, backend, values.Length);
        }

        var midpoint = values.Length / 2;
        return values.Length % 2 == 0
            ? (values[midpoint - 1] + values[midpoint]) / 2.0
            : values[midpoint];
    }

    private static void CheckMaximum(
        string name,
        double actual,
        double maximum,
        string unit,
        List<string> failures)
    {
        if (!double.IsFinite(actual) || actual <= 0)
        {
            failures.Add($"{name}: value must be greater than zero");
            return;
        }

        var message = string.Create(
            CultureInfo.InvariantCulture,
            $"{name}: {actual:F3} {unit} <= {maximum:F3} {unit}");
        if (actual <= maximum)
        {
            Console.WriteLine($"PASS {message}");
        }
        else
        {
            failures.Add(message);
        }
    }

    private static void CheckExact(
        string name,
        long actual,
        long expected,
        List<string> failures)
    {
        var message = $"{name}: {actual} B == {expected} B";
        if (actual == expected)
        {
            Console.WriteLine($"PASS {message}");
        }
        else
        {
            failures.Add(message);
        }
    }

    private static double ParseDurationMicroseconds(string path, string column, string value)
    {
        var separator = value.LastIndexOf(' ');
        if (separator <= 0)
        {
            throw GateThrowHelper.InvalidValue(path, column, value);
        }

        var number = ParsePositiveDouble(path, column, value[..separator]);
        return value[(separator + 1)..] switch
        {
            "ns" => number / 1_000.0,
            "us" or "μs" => number,
            "ms" => number * 1_000.0,
            "s" => number * 1_000_000.0,
            _ => throw GateThrowHelper.InvalidValue(path, column, value)
        };
    }

    private static long ParseBytes(string path, string column, string value)
    {
        var separator = value.LastIndexOf(' ');
        if (separator <= 0)
        {
            throw GateThrowHelper.InvalidValue(path, column, value);
        }

        var number = ParseDouble(path, column, value[..separator]);
        var multiplier = value[(separator + 1)..] switch
        {
            "B" => 1.0,
            "KB" => 1_024.0,
            "MB" => 1_048_576.0,
            "GB" => 1_073_741_824.0,
            _ => throw GateThrowHelper.InvalidValue(path, column, value)
        };
        var bytes = number * multiplier;
        if (bytes < 0 || bytes > long.MaxValue || bytes != Math.Truncate(bytes))
        {
            throw GateThrowHelper.InvalidValue(path, column, value);
        }

        return (long)bytes;
    }

    private static int ParseInteger(string path, string column, string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw GateThrowHelper.InvalidValue(path, column, value);
        }

        return result;
    }

    private static double ParseDouble(string path, string column, string value)
    {
        if (!double.TryParse(
                value,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out var result)
            || !double.IsFinite(result))
        {
            throw GateThrowHelper.InvalidValue(path, column, value);
        }

        return result;
    }

    private static double ParsePositiveDouble(string path, string column, string value)
    {
        var result = ParseDouble(path, column, value);
        if (result <= 0)
        {
            throw GateThrowHelper.InvalidValue(path, column, value);
        }

        return result;
    }

    private static int GetFullyCoveredVariableCount(int caseBudget)
    {
        var value = (long)caseBudget + 2;
        var variableCount = 0;

        while (value > 1 && (value & 1) == 0)
        {
            value >>= 1;
            variableCount++;
        }

        if (value != 1 || variableCount <= 0)
        {
            throw GateThrowHelper.InvalidConfiguration(
                "caseBudget must equal 2 * (2^n - 1) for the two-region adversarial case");
        }

        return variableCount - 1;
    }

    private static GateConfiguration ReadConfiguration(string path)
    {
        var configuration = JsonSerializer.Deserialize<GateConfiguration>(
            File.ReadAllText(path),
            JsonSerializerOptions.Web);
        return configuration ?? throw GateThrowHelper.ConfigurationCouldNotBeRead(path);
    }

    private static void ValidateEngineDefault(
        string repositoryRoot,
        GateConfiguration configuration)
    {
        var path = Path.Combine(repositoryRoot, EngineOptionsFile);
        var source = File.ReadAllText(path);
        var match = CaseBudgetDefaultRegex().Match(source);
        if (!match.Success
            || !int.TryParse(
                match.Groups[1].Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var engineDefault)
            || engineDefault != configuration.CaseBudget)
        {
            throw GateThrowHelper.InvalidConfiguration(
                $"caseBudget {configuration.CaseBudget} does not match the engine default in '{path}'");
        }
    }

    private static void ValidateHeadToHeadProvenance(
        string repositoryRoot,
        GateConfiguration configuration)
    {
        var path = Path.Combine(repositoryRoot, HeadToHeadProvenanceFile);
        var provenance = JsonSerializer.Deserialize<HeadToHeadProvenance>(
            File.ReadAllText(path),
            JsonSerializerOptions.Web)
            ?? throw GateThrowHelper.ProvenanceCouldNotBeRead(path);
        if (!GitRevisionRegex().IsMatch(provenance.GitRev)
            || provenance.OracleGitRev != OracleRevision
            || provenance.Selection != "endpoints"
            || provenance.Replicates != CampaignReplicates
            || provenance.Seed != CampaignSeed)
        {
            throw GateThrowHelper.ProvenanceCouldNotBeRead(path);
        }

        if (!RevisionResolves(repositoryRoot, provenance.GitRev))
        {
            throw GateThrowHelper.ProvenanceCouldNotBeRead(path);
        }

        ValidateFrozenRevision(repositoryRoot, configuration, provenance.GitRev);

        var currentRevision = ReadCurrentRevision(repositoryRoot);
        var workingTreeChanges = ReadWorkingTreePaths(repositoryRoot);
        if (provenance.GitRev == currentRevision)
        {
            if (!ContainsOnlyGeneratedArtifacts(workingTreeChanges))
            {
                throw GateThrowHelper.UnexpectedRepositoryChanges(path);
            }

            return;
        }

        if (!IsAncestor(repositoryRoot, provenance.GitRev, currentRevision))
        {
            throw GateThrowHelper.InvalidCommittedProvenance(path);
        }

        var artifactChanges = ReadCommittedPaths(repositoryRoot, provenance.GitRev);
        if (workingTreeChanges.Length != 0
            || artifactChanges.Length == 0
            || !ContainsOnlyGeneratedArtifacts(artifactChanges))
        {
            throw GateThrowHelper.InvalidCommittedProvenance(path);
        }
    }

    private static bool ContainsOnlyGeneratedArtifacts(IEnumerable<string> paths)
        => paths.All(s_generatedArtifactPaths.Contains);

    private static void ValidateFrozenRevision(
        string repositoryRoot,
        GateConfiguration configuration,
        string measuredRevision)
    {
        if (!RevisionResolves(repositoryRoot, configuration.FrozenAt))
        {
            throw GateThrowHelper.InvalidConfiguration(
                $"frozenAt revision {configuration.FrozenAt} does not resolve");
        }

        if (!IsAncestor(repositoryRoot, configuration.FrozenAt, measuredRevision))
        {
            throw GateThrowHelper.InvalidConfiguration(
                $"frozenAt revision {configuration.FrozenAt} is not an ancestor of "
                + $"measured revision {measuredRevision}");
        }
    }

    private static string ReadCurrentRevision(string repositoryRoot)
    {
        var result = RunGit(repositoryRoot, "rev-parse", "HEAD");
        var output = result.Output.Trim();
        if (result.ExitCode != 0 || !GitRevisionRegex().IsMatch(output))
        {
            throw GateThrowHelper.GitRevisionCouldNotBeRead(result.Error);
        }

        return output;
    }

    private static bool RevisionResolves(string repositoryRoot, string revision)
    {
        var result = RunGit(repositoryRoot, "rev-parse", "--verify", $"{revision}^{{commit}}");
        return result.ExitCode == 0
            && result.Output.Trim().Equals(revision, StringComparison.Ordinal);
    }

    private static bool IsAncestor(
        string repositoryRoot,
        string revision,
        string descendant)
    {
        var result = RunGit(
            repositoryRoot,
            "merge-base",
            "--is-ancestor",
            revision,
            descendant);
        return result.ExitCode switch
        {
            0 => true,
            1 => false,
            _ => throw GateThrowHelper.GitRevisionCouldNotBeRead(result.Error)
        };
    }

    private static string[] ReadWorkingTreePaths(string repositoryRoot)
    {
        var tracked = ReadGitPaths(
            RunGit(
                repositoryRoot,
                "diff",
                "--name-only",
                "--no-renames",
                "-z",
                "HEAD",
                "--"));
        var untracked = ReadGitPaths(
            RunGit(
                repositoryRoot,
                "ls-files",
                "--others",
                "--exclude-standard",
                "-z",
                "--"));
        return [.. tracked, .. untracked];
    }

    private static string[] ReadGitPaths(GitResult result)
    {
        if (result.ExitCode != 0)
        {
            throw GateThrowHelper.GitRevisionCouldNotBeRead(result.Error);
        }

        if (result.Output.Length == 0)
        {
            return [];
        }

        if (result.Output[^1] != '\0')
        {
            throw GateThrowHelper.InvalidGitPathOutput();
        }

        var paths = result.Output[..^1].Split('\0');
        if (paths.Any(string.IsNullOrEmpty))
        {
            throw GateThrowHelper.InvalidGitPathOutput();
        }

        return paths;
    }

    private static string[] ReadCommittedPaths(string repositoryRoot, string revision)
    {
        var result = RunGit(
            repositoryRoot,
            "diff",
            "--name-only",
            "--no-renames",
            "-z",
            $"{revision}..HEAD",
            "--");
        return ReadGitPaths(result);
    }

    private static GitResult RunGit(string repositoryRoot, params string[] arguments)
    {
        using var process = new Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.ArgumentList.Add("-C");
        process.StartInfo.ArgumentList.Add(repositoryRoot);
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;

        if (!process.Start())
        {
            throw GateThrowHelper.GitRevisionCouldNotBeRead("git did not start");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();
        return new GitResult(process.ExitCode, output, error);
    }

    private static void ValidateConfiguration(GateConfiguration configuration)
    {
        if (configuration.WarmEvaluateP50Us != 1.0)
        {
            throw GateThrowHelper.InvalidConfiguration("warmEvaluateP50Us must be 1");
        }

        if (configuration.WarmAllocatedBytes != 0)
        {
            throw GateThrowHelper.InvalidConfiguration("warmAllocatedBytes must be 0");
        }

        if (configuration.ColdRustBand != 2.0)
        {
            throw GateThrowHelper.InvalidConfiguration("coldRustBand must be 2.0");
        }

        if (configuration.AdversarialMs != 1.0)
        {
            throw GateThrowHelper.InvalidConfiguration("adversarialMs must be 1");
        }

        if (configuration.CaseBudget <= 0)
        {
            throw GateThrowHelper.InvalidConfiguration("caseBudget must be positive");
        }

        if (!GitRevisionRegex().IsMatch(configuration.FrozenAt))
        {
            throw GateThrowHelper.InvalidConfiguration("frozenAt must be a full Git revision");
        }

        _ = GetFullyCoveredVariableCount(configuration.CaseBudget);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null)
        {
            if ((Directory.Exists(Path.Combine(directory.FullName, ".git"))
                    || File.Exists(Path.Combine(directory.FullName, ".git")))
                && Directory.Exists(Path.Combine(directory.FullName, ".github")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw GateThrowHelper.RepositoryRootNotFound();
    }

    [GeneratedRegex("^[0-9a-f]{40}$", RegexOptions.CultureInvariant)]
    private static partial Regex GitRevisionRegex();

    [GeneratedRegex(@"CaseBudget\s*\{\s*get;\s*set;\s*\}\s*=\s*(\d+);")]
    private static partial Regex CaseBudgetDefaultRegex();

    private readonly record struct Endpoint(
        int ObjectTypes,
        int AbstractTypes,
        int IncidencesPerObject,
        int QuerySpreads,
        int TypeCost,
        int FieldCost);

    private readonly record struct GitResult(int ExitCode, string Output, string Error);
}
