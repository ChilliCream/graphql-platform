using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

internal static class HeadToHeadPointRunner
{
    private const int SampleCount = 5;
    private const long TargetSampleNanoseconds = 100_000_000;
    private const long StableCalibrationNanoseconds = 2 * TargetSampleNanoseconds;
    private const int MaxCalibrationIterations = 67_108_864;
    private const string OracleCommit = "fec57fd7a980b5399637fa464a9bdce0781d91dd";
    private const string Header =
        "backend,object_types,abstract_types,incidences_per_object,query_spreads,type_cost,field_cost,iterations,median_total_ns,median_ns_per_op,sample_0_total_ns,sample_1_total_ns,sample_2_total_ns,sample_3_total_ns,sample_4_total_ns,checksum";

    public static bool TryRun(string[] args, out int exitCode)
    {
        if (args.Length == 0
            || !args[0].Equals("head-to-head-point", StringComparison.OrdinalIgnoreCase))
        {
            exitCode = 0;
            return false;
        }

        if (args.Length != 4
            || !Enum.TryParse<MeasurementPhase>(args[3], ignoreCase: true, out var phase))
        {
            Console.Error.WriteLine(
                "Usage: head-to-head-point CORPUS SCENARIO_ID Cold|Warm");
            exitCode = 2;
            return true;
        }

        Run(args[1], args[2], phase);
        exitCode = 0;
        return true;
    }

    private static void Run(string corpusPath, string scenarioId, MeasurementPhase phase)
    {
        var corpus = HeadToHeadCorpus.Load(corpusPath);
        if (!corpus.SourceCommit.Equals(OracleCommit, StringComparison.Ordinal))
        {
            HeadToHeadThrowHelper.InvalidSourceCommit(corpus.SourceCommit);
        }

        if (corpus.DefaultListSize != 1)
        {
            HeadToHeadThrowHelper.InvalidDefaultListSize(corpus.DefaultListSize);
        }

        var scenario = corpus.Scenarios.SingleOrDefault(t => t.Id.Equals(scenarioId, StringComparison.Ordinal))
            ?? HeadToHeadThrowHelper.ScenarioNotFound(scenarioId);
        if (!scenario.Axis.Equals("pathological-booleans", StringComparison.Ordinal)
            && (scenario.ExpectedTypeCost != 2.0 || scenario.ExpectedFieldCost != 2.0))
        {
            HeadToHeadThrowHelper.InvalidTopologyExpectation(
                scenario.Id,
                scenario.ExpectedTypeCost,
                scenario.ExpectedFieldCost);
        }

        var schema = SchemaParser.Parse(scenario.Schema);
        var snapshot = CostSchemaSnapshot.Create(
            schema,
            new CostEngineOptions { DefaultListSize = 1 });
        var (document, operation) = BenchmarkFixture.ParseOperationSource(scenario.Operation);
        var variables = CreateVariables(scenario.Variables);
        var warmPlan = Compile(snapshot, document, operation);

        var initial = phase is MeasurementPhase.Cold
            ? Compile(snapshot, document, operation).Evaluate(variables)
            : warmPlan.Evaluate(variables);
        EnsureExpected(scenario, initial);

        RunIterations(phase, snapshot, document, operation, warmPlan, variables, 2);
        var iterations = Calibrate(phase, snapshot, document, operation, warmPlan, variables);
        var samples = new long[SampleCount];
        long medianTotalNanoseconds;
        ulong checksum;

        while (true)
        {
            checksum = 0;
            for (var index = 0; index < samples.Length; index++)
            {
                (samples[index], var sampleChecksum) = Timed(
                    phase,
                    snapshot,
                    document,
                    operation,
                    warmPlan,
                    variables,
                    iterations);
                checksum = unchecked(checksum + sampleChecksum);
            }

            var sortedSamples = samples.ToArray();
            Array.Sort(sortedSamples);
            medianTotalNanoseconds = sortedSamples[sortedSamples.Length / 2];
            if (medianTotalNanoseconds >= TargetSampleNanoseconds
                || iterations >= MaxCalibrationIterations)
            {
                break;
            }

            iterations *= 2;
        }

        var backend = phase is MeasurementPhase.Cold
            ? "hotchocolate-cold"
            : "hotchocolate-warm";

        Console.WriteLine(Header);
        Console.WriteLine(string.Join(
            ',',
            backend,
            scenario.ObjectTypes.ToString(CultureInfo.InvariantCulture),
            scenario.AbstractTypes.ToString(CultureInfo.InvariantCulture),
            scenario.IncidencesPerObject.ToString(CultureInfo.InvariantCulture),
            scenario.QuerySpreads.ToString(CultureInfo.InvariantCulture),
            initial.TypeCost.ToString("R", CultureInfo.InvariantCulture),
            initial.FieldCost.ToString("R", CultureInfo.InvariantCulture),
            iterations.ToString(CultureInfo.InvariantCulture),
            medianTotalNanoseconds.ToString(CultureInfo.InvariantCulture),
            (medianTotalNanoseconds / iterations).ToString(CultureInfo.InvariantCulture),
            samples[0].ToString(CultureInfo.InvariantCulture),
            samples[1].ToString(CultureInfo.InvariantCulture),
            samples[2].ToString(CultureInfo.InvariantCulture),
            samples[3].ToString(CultureInfo.InvariantCulture),
            samples[4].ToString(CultureInfo.InvariantCulture),
            checksum.ToString(CultureInfo.InvariantCulture)));
    }

    private static int Calibrate(
        MeasurementPhase phase,
        CostSchemaSnapshot snapshot,
        DocumentNode document,
        OperationDefinitionNode operation,
        CostPlan warmPlan,
        BenchmarkVariableValues variables)
    {
        var iterations = 1;
        while (true)
        {
            var (elapsedNanoseconds, _) = Timed(
                phase,
                snapshot,
                document,
                operation,
                warmPlan,
                variables,
                iterations);
            if (elapsedNanoseconds >= TargetSampleNanoseconds
                || iterations >= MaxCalibrationIterations)
            {
                var (confirmationNanoseconds, _) = Timed(
                    phase,
                    snapshot,
                    document,
                    operation,
                    warmPlan,
                    variables,
                    iterations);
                if (confirmationNanoseconds >= StableCalibrationNanoseconds
                    || iterations >= MaxCalibrationIterations)
                {
                    return iterations;
                }
            }

            iterations *= 2;
        }
    }

    private static (long ElapsedNanoseconds, ulong Checksum) Timed(
        MeasurementPhase phase,
        CostSchemaSnapshot snapshot,
        DocumentNode document,
        OperationDefinitionNode operation,
        CostPlan warmPlan,
        BenchmarkVariableValues variables,
        int iterations)
    {
        var start = Stopwatch.GetTimestamp();
        var checksum = RunIterations(
            phase,
            snapshot,
            document,
            operation,
            warmPlan,
            variables,
            iterations);
        var elapsedTicks = Stopwatch.GetTimestamp() - start;
        var elapsedNanoseconds = elapsedTicks * 1_000_000_000L / Stopwatch.Frequency;
        return (elapsedNanoseconds, checksum);
    }

    private static ulong RunIterations(
        MeasurementPhase phase,
        CostSchemaSnapshot snapshot,
        DocumentNode document,
        OperationDefinitionNode operation,
        CostPlan warmPlan,
        BenchmarkVariableValues variables,
        int iterations)
    {
        ulong checksum = 14_695_981_039_346_656_037;
        for (var index = 0; index < iterations; index++)
        {
            var result = phase is MeasurementPhase.Cold
                ? Compile(snapshot, document, operation).Evaluate(variables)
                : warmPlan.Evaluate(variables);
            checksum = unchecked(
                (checksum ^ (ulong)BitConverter.DoubleToInt64Bits(result.TypeCost))
                * 1_099_511_628_211);
            checksum = unchecked(
                (checksum ^ (ulong)BitConverter.DoubleToInt64Bits(result.FieldCost))
                * 1_099_511_628_211);
        }

        return checksum;
    }

    private static CostPlan Compile(
        CostSchemaSnapshot snapshot,
        DocumentNode document,
        OperationDefinitionNode operation)
        => CostPlanCompiler.Compile(snapshot, document, operation, CostAnalyses.Cost);

    private static BenchmarkVariableValues CreateVariables(JsonElement variables)
    {
        var values = new List<(string Name, IValueNode Value)>();
        foreach (var property in variables.EnumerateObject())
        {
            if (property.Value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            {
                HeadToHeadThrowHelper.UnsupportedVariableValue(property.Name);
            }

            values.Add((property.Name, new BooleanValueNode(property.Value.GetBoolean())));
        }

        return BenchmarkFixture.Variables(values.ToArray());
    }

    private static void EnsureExpected(HeadToHeadScenario scenario, CostEstimate actual)
    {
        if (actual.TypeCost != scenario.ExpectedTypeCost
            || actual.FieldCost != scenario.ExpectedFieldCost)
        {
            HeadToHeadThrowHelper.CostMismatch(
                scenario.Id,
                scenario.ExpectedTypeCost,
                scenario.ExpectedFieldCost,
                actual.TypeCost,
                actual.FieldCost);
        }
    }

    private enum MeasurementPhase
    {
        Cold,
        Warm
    }
}
