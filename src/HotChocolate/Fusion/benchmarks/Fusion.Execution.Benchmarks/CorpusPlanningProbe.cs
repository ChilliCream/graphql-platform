using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Benchmarks;

// One-shot feasibility probe for the composed corpus (Phase A).
// Not a BenchmarkDotNet benchmark: it measures cold-start schema build and a
// single plan per query with wall time + allocation deltas, so it can report a
// "wall" (non-termination / crash) on any layer as a first-class finding.
internal static class CorpusPlanningProbe
{
    public static void Run(string[] args)
    {
        var schemaPath = args.Length > 1
            ? args[1]
            : CorpusPaths.SchemaPath;
        var query1Path = args.Length > 2
            ? args[2]
            : CorpusPaths.Query1Path;
        var query2Path = args.Length > 3
            ? args[3]
            : CorpusPaths.Query2Path;

        Console.WriteLine("=== corpus planning feasibility probe ===");
        Console.WriteLine($"runtime          : {Environment.Version}, ServerGC={System.Runtime.GCSettings.IsServerGC}");
        Console.WriteLine($"processors       : {Environment.ProcessorCount}");
        Console.WriteLine($"schema           : {schemaPath}");
        Console.WriteLine();

        // 1) Read + parse the composed schema document.
        var schemaText = File.ReadAllText(schemaPath);
        Console.WriteLine($"schema file size : {schemaText.Length / (1024.0 * 1024.0):F1} MiB");

        DocumentNode schemaDoc = null!;
        Measure("parse schema SDL", () =>
        {
            schemaDoc = Utf8GraphQLParser.Parse(schemaText);
        });
        Console.WriteLine($"schema defs      : {schemaDoc.Definitions.Count}");
        Console.WriteLine();

        // 2) Build the Fusion schema definition (cold start).
        // Bounded by a watchdog: FusionSchemaDefinition.Create has an unbounded
        // schema-completion step (PlannerTopologyCache.BuildTransitions) that grows
        // super-linearly with the source-schema count, so on a large corpus it can
        // exhaust host memory before returning. If the build overruns the budget we
        // report a wall and exit rather than let the process OOM the host.
        var buildBudget = TimeSpan.FromSeconds(
            int.TryParse(Environment.GetEnvironmentVariable("SCHEMA_BUILD_BUDGET_SECONDS"), out var s)
                ? s
                : 120);

        FusionSchemaDefinition schema = null!;
        if (!MeasureBounded(
                "build FusionSchemaDefinition",
                () => schema = FusionSchemaDefinition.Create(schemaDoc),
                buildBudget))
        {
            Console.WriteLine(
                $"  !! WALL: schema build did not complete within {buildBudget.TotalSeconds:F0}s.");
            Console.WriteLine(
                "     Hot path: PlannerTopologyCache.BuildTransitions "
                + "(O(complexTypes x sourceSchemas^2)); planning is unreachable on this corpus.");
            Console.WriteLine("=== probe halted at schema build ===");
            Environment.Exit(2);
        }

        Console.WriteLine($"schema types     : {CountTypes(schema)}");
        Console.WriteLine();

        // Shared planning infrastructure.
        var rewriter = new DocumentRewriter(schema);
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var compiler = new OperationCompiler(schema, pool);

        // Guard against non-termination: bound planning so a wall surfaces as a
        // thrown guardrail rather than an infinite hang.
        var options = new OperationPlannerOptions
        {
            MaxPlanningTime = TimeSpan.FromMinutes(3)
        };
        var planner = new OperationPlanner(schema, compiler, options);

        ProbeQuery(planner, rewriter, "Query1", query1Path);
        ProbeQuery(planner, rewriter, "Query2", query2Path);

        Console.WriteLine("=== probe complete ===");
    }

    private static void ProbeQuery(
        OperationPlanner planner,
        DocumentRewriter rewriter,
        string label,
        string path)
    {
        Console.WriteLine($"--- {label} ({path}) ---");
        var text = File.ReadAllText(path);
        Console.WriteLine($"query size       : {text.Length / 1024.0:F1} KiB");

        DocumentNode doc = null!;
        Measure($"{label}: parse", () => doc = Utf8GraphQLParser.Parse(text));

        OperationDefinitionNode operation = null!;
        Measure($"{label}: rewrite+getOperation", () =>
        {
            operation = rewriter.RewriteDocument(doc).GetOperation(operationName: null);
        });

        try
        {
            OperationPlan plan = null!;
            Measure($"{label}: CreatePlan", () =>
            {
                // id, hash and shortHash mirror what the request pipeline supplies.
                // The short hash is threaded into synthesized lookup operation names
                // (Op_{shortHash}_{stepId}), so it must be a valid GraphQL name
                // component, and the plan formatter slices hash[..8], so the hash
                // must be at least eight name-safe characters. A hex document hash
                // satisfies both (a hyphen would re-parse as a number).
                var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
                plan = planner.CreatePlan(label, hash, hash[..8], operation);
            });
            Console.WriteLine($"  searchSpace    : {plan.SearchSpace}");
            Console.WriteLine($"  expandedNodes  : {plan.ExpandedNodes}");
            Console.WriteLine($"  executionNodes : {plan.AllNodes.Length}");
            Console.WriteLine($"  maxNodeId      : {plan.MaxNodeId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  !! WALL on {label}: {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static int CountTypes(FusionSchemaDefinition schema)
    {
        var count = 0;
        foreach (var _ in schema.Types)
        {
            count++;
        }
        return count;
    }

    // Runs action on a background thread and waits at most budget for it. Returns
    // true if it completed in time (and prints the same timing line as Measure),
    // false if it overran. A false return leaves the worker thread running; the
    // caller is expected to exit the process, which tears the thread down.
    private static bool MeasureBounded(string label, Action action, TimeSpan budget)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocBefore = GC.GetTotalAllocatedBytes(precise: false);
        var heapBefore = GC.GetTotalMemory(forceFullCollection: false);
        var sw = Stopwatch.StartNew();

        var worker = new Thread(() => action())
        {
            IsBackground = true,
            Name = label
        };
        worker.Start();

        if (!worker.Join(budget))
        {
            sw.Stop();
            return false;
        }

        sw.Stop();
        var allocAfter = GC.GetTotalAllocatedBytes(precise: false);
        var heapAfter = GC.GetTotalMemory(forceFullCollection: false);

        Console.WriteLine(
            $"{label,-34} : {sw.Elapsed.TotalMilliseconds,10:F1} ms" +
            $" | alloc {(allocAfter - allocBefore) / (1024.0 * 1024.0),9:F1} MiB" +
            $" | heap {(heapAfter - heapBefore) / (1024.0 * 1024.0),9:F1} MiB");
        return true;
    }

    private static void Measure(string label, Action action)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocBefore = GC.GetTotalAllocatedBytes(precise: false);
        var heapBefore = GC.GetTotalMemory(forceFullCollection: false);
        var sw = Stopwatch.StartNew();

        action();

        sw.Stop();
        var allocAfter = GC.GetTotalAllocatedBytes(precise: false);
        var heapAfter = GC.GetTotalMemory(forceFullCollection: false);

        Console.WriteLine(
            $"{label,-34} : {sw.Elapsed.TotalMilliseconds,10:F1} ms" +
            $" | alloc {(allocAfter - allocBefore) / (1024.0 * 1024.0),9:F1} MiB" +
            $" | heap {(heapAfter - heapBefore) / (1024.0 * 1024.0),9:F1} MiB");
    }
}
