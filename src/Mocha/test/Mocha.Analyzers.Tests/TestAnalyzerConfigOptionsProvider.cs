using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mocha.Analyzers.Tests;

internal sealed class TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> globalOptions)
    : AnalyzerConfigOptionsProvider
{
    private readonly AnalyzerConfigOptions _globalOptions = new TestAnalyzerConfigOptions(globalOptions);

    public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        => TestAnalyzerConfigOptions.Empty;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        => TestAnalyzerConfigOptions.Empty;
}

internal sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> options)
    : AnalyzerConfigOptions
{
    public static readonly AnalyzerConfigOptions Empty =
        new TestAnalyzerConfigOptions(new Dictionary<string, string>());

    public override bool TryGetValue(string key, out string value)
    {
        if (options.TryGetValue(key, out var result))
        {
            value = result;
            return true;
        }

        value = "";
        return false;
    }
}
