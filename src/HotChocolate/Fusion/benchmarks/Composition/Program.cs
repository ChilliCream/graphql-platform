using System.Diagnostics;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Composition.Benchmarks;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

var subgraphsPath = args.Length > 0 ? args[0] : CorpusPaths.SubgraphsPath;
var printErrors = args.Contains("--errors");

var files = Directory.GetFiles(subgraphsPath, "*.graphqls");

if (files.Length == 0)
{
    Console.Error.WriteLine($"No .graphqls files found in '{subgraphsPath}'.");
    return 1;
}

var sourceSchemas = new List<SourceSchemaText>(files.Length);

foreach (var file in files)
{
    var name = Path.GetFileNameWithoutExtension(file);
    sourceSchemas.Add(new SourceSchemaText(name, File.ReadAllText(file)));
}

var options = new SchemaComposerOptions();

foreach (var sourceSchema in sourceSchemas)
{
    options.SourceSchemas[sourceSchema.Name] = new SourceSchemaOptions
    {
        Preprocessor = new SourceSchemaPreprocessorOptions { InferKeysFromLookups = false }
    };
}

var log = new CompositionLog();
var stopwatch = Stopwatch.StartNew();
var result = new SchemaComposer(sourceSchemas, options, log).Compose();
stopwatch.Stop();

var unsatisfiable = log
    .Where(e => e.Code == LogEntryCodes.UnsatisfiableQueryPath)
    .Select(e => e.Message)
    .ToList();

Console.WriteLine($"subgraphs               : {files.Length}");
Console.WriteLine($"outcome                 : {(result.IsSuccess ? "SUCCESS" : "FAILURE")}");
Console.WriteLine($"wall time               : {stopwatch.Elapsed.TotalSeconds:0.00}s");
Console.WriteLine($"UNSATISFIABLE_QUERY_PATH: {unsatisfiable.Count}");
Console.WriteLine($"total error entries     : {log.Count(e => e.Severity == LogSeverity.Error)}");
Console.WriteLine("error codes (severity=Error):");

foreach (var group in log
    .Where(e => e.Severity == LogSeverity.Error)
    .GroupBy(e => e.Code)
    .OrderByDescending(g => g.Count()))
{
    Console.WriteLine($"  {group.Count(),5}  {group.Key}");
    Console.WriteLine($"         e.g. {group.First().Message}");
}

if (printErrors)
{
    foreach (var message in unsatisfiable)
    {
        Console.WriteLine("=====");
        Console.WriteLine(message);
    }
}

return result.IsSuccess ? 0 : 2;
