using System.Diagnostics;
using System.Text;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.ApolloFederation;

// THROWAWAY corpus probe (Query-less fix verification). Delete after the re-run.
public sealed class CorpusScaleProbe
{
    private const string SubgraphsDir =
        "/Users/michael/local/big-federated-graphs/schemas/edge0-v2/subgraphs";

    private const string ReportPath =
        "/private/tmp/claude-501/-Users-michael-local-hc-1-repo/dd0e966b-0c00-47c0-8d06-02fbf51d3d57/scratchpad/corpus-report.txt";

    private const string ErrorsPath =
        "/private/tmp/claude-501/-Users-michael-local-hc-1-repo/dd0e966b-0c00-47c0-8d06-02fbf51d3d57/scratchpad/corpus-errors.txt";

    [Fact]
    public void Compose_Edge0V2()
    {
        var files = System.IO.Directory.GetFiles(SubgraphsDir, "*.graphqls");
        var sourceSchemas = new List<SourceSchemaText>(files.Length);
        foreach (var file in files)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(file);
            sourceSchemas.Add(new SourceSchemaText(name, System.IO.File.ReadAllText(file)));
        }

        var options = new SchemaComposerOptions();
        foreach (var s in sourceSchemas)
        {
            options.SourceSchemas[s.Name] = new SourceSchemaOptions
            {
                Preprocessor = new SourceSchemaPreprocessorOptions { InferKeysFromLookups = false }
            };
        }

        var log = new CompositionLog();
        var sw = Stopwatch.StartNew();
        var result = new SchemaComposer(sourceSchemas, options, log).Compose();
        sw.Stop();

        var unsat = log.Where(e => e.Code == LogEntryCodes.UnsatisfiableQueryPath).Select(e => e.Message).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("=== edge0-v2 (after Query-less lookup fix) ===");
        sb.AppendLine($"subgraphs               : {files.Length}");
        sb.AppendLine($"outcome                 : {(result.IsSuccess ? "SUCCESS" : "FAILURE")}");
        sb.AppendLine($"wall time               : {sw.Elapsed.TotalSeconds:0.00}s");
        sb.AppendLine($"UNSATISFIABLE_QUERY_PATH: {unsat.Count}");
        sb.AppendLine($"total error entries     : {log.Count(e => e.Severity == LogSeverity.Error)}");
        sb.AppendLine("error codes (severity=Error):");
        foreach (var g in log.Where(e => e.Severity == LogSeverity.Error)
            .GroupBy(e => e.Code).OrderByDescending(g => g.Count()))
        {
            sb.AppendLine($"  {g.Count()}  {g.Key}: {g.First().Message}");
        }
        System.IO.File.WriteAllText(ReportPath, sb.ToString());

        var eb = new StringBuilder();
        eb.AppendLine($"total: {unsat.Count}");
        foreach (var m in unsat)
        {
            eb.AppendLine("=====");
            eb.AppendLine(m);
        }
        System.IO.File.WriteAllText(ErrorsPath, eb.ToString());

        Assert.True(files.Length > 0);
    }
}
