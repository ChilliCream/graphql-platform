using System.Reflection;
using System.Text.Json;

namespace HotChocolate.Fusion;

/// <summary>
/// Reads embedded reference files (<c>tests.json</c>, <c>data.json</c>,
/// <c>subgraphs/*.graphql</c>) vendored from the federation-gateway-audit repository
/// under each suite's <c>Reference/</c> directory. Reference resources are material
/// for porting a suite; the live tests inline their query literals via
/// <see cref="ComplianceTestBase.RunAsync"/>.
/// </summary>
internal static class AuditFixture
{
    private const string SuitesPath = "Suites";
    private const string ReferencePath = "Reference";
    private const string OfficialV1ManifestResource =
        "HotChocolate.Fusion.OfficialAudit.v1-manifest.json";
    private const string OfficialV2ManifestResource =
        "HotChocolate.Fusion.OfficialAudit.v2-manifest.json";
    private static readonly Assembly s_assembly = typeof(AuditFixture).Assembly;
    private static readonly string s_assemblyName = s_assembly.GetName().Name!;
    private static readonly Lazy<OfficialV1Manifest> s_officialV1Manifest =
        new(LoadOfficialV1Manifest);
    private static readonly Lazy<OfficialV2Manifest> s_officialV2Manifest =
        new(LoadOfficialV2Manifest);

    public static TheoryData<string> GetOfficialV1CaseIds<TSuite>()
    {
        var suite = GetOfficialV1Suite<TSuite>();
        var cases = new TheoryData<string>();

        foreach (var testCase in suite.Cases)
        {
            cases.Add(testCase.Id);
        }

        return cases;
    }

    public static AuditTestCase GetOfficialV1Case<TSuite>(string caseId)
    {
        ArgumentException.ThrowIfNullOrEmpty(caseId);

        return GetOfficialV1Suite<TSuite>().Cases.Single(testCase => testCase.Id == caseId);
    }

    public static IReadOnlyList<OfficialSourceSchema> GetOfficialV1SourceSchemas<TSuite>()
        => GetOfficialV1Suite<TSuite>().Sources;

    public static OfficialV1Manifest GetOfficialV1Manifest()
        => s_officialV1Manifest.Value;

    public static OfficialV1SuiteAttribute GetOfficialV1SuiteAttribute<TSuite>()
        => typeof(TSuite)
            .GetCustomAttribute<OfficialV1SuiteAttribute>()
            ?? throw new InvalidOperationException(
                $"Test class '{typeof(TSuite).FullName}' has no OfficialV1SuiteAttribute.");

    public static TheoryData<string> GetOfficialV2CaseIds<TSuite>()
    {
        var suite = GetOfficialV2Suite<TSuite>();
        var cases = new TheoryData<string>();

        foreach (var testCase in suite.Cases)
        {
            cases.Add(testCase.Id);
        }

        return cases;
    }

    public static AuditTestCase GetOfficialV2Case<TSuite>(string caseId)
    {
        ArgumentException.ThrowIfNullOrEmpty(caseId);

        return GetOfficialV2Suite<TSuite>().Cases.Single(testCase => testCase.Id == caseId);
    }

    public static AuditTestCase GetOfficialV2Case(string caseId)
    {
        ArgumentException.ThrowIfNullOrEmpty(caseId);

        return s_officialV2Manifest.Value.Suites
            .SelectMany(suite => suite.Cases)
            .Single(testCase => testCase.Id == caseId);
    }

    public static IReadOnlyList<OfficialSourceSchema> GetOfficialV2SourceSchemas<TSuite>()
        => GetOfficialV2Suite<TSuite>().Sources;

    public static OfficialV2Manifest GetOfficialV2Manifest()
        => s_officialV2Manifest.Value;

    public static OfficialV2SuiteAttribute GetOfficialV2SuiteAttribute<TSuite>()
        => typeof(TSuite)
            .GetCustomAttribute<OfficialV2SuiteAttribute>()
            ?? throw new InvalidOperationException(
                $"Test class '{typeof(TSuite).FullName}' has no OfficialV2SuiteAttribute.");

    /// <summary>
    /// Reads an embedded reference text file for the specified audit suite.
    /// </summary>
    /// <param name="suiteName">
    /// The suite's PascalCase folder name under <c>Suites/</c>
    /// (e.g. <c>"SimpleEntityCall"</c>).
    /// </param>
    /// <param name="relativePath">
    /// The path under the suite's <c>Reference/</c> directory
    /// (e.g. <c>"tests.json"</c> or <c>"email.graphql"</c>). Forward slashes are
    /// translated to embedded-resource path separators.
    /// </param>
    /// <returns>The UTF-8 contents of the embedded resource.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the expected embedded resource cannot be located.
    /// </exception>
    public static string LoadText(string suiteName, string relativePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(suiteName);
        ArgumentException.ThrowIfNullOrEmpty(relativePath);

        var resourceName = BuildResourceName(suiteName, relativePath);

        using var stream = s_assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"Embedded reference resource '{resourceName}' was not found.",
                resourceName);

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string BuildResourceName(string suiteName, string relativePath)
    {
        // Embedded resource names replace directory separators with '.', so
        // 'subgraphs/email.graphql' becomes 'subgraphs.email.graphql'.
        var normalized = relativePath.Replace('/', '.').Replace('\\', '.');
        return $"{s_assemblyName}.{SuitesPath}.{suiteName}.{ReferencePath}.{normalized}";
    }

    internal static OfficialAuditSuite GetOfficialV1Suite<TSuite>()
    {
        var suiteId = GetOfficialV1SuiteAttribute<TSuite>().Id;

        return s_officialV1Manifest.Value.Suites.Single(suite => suite.Id == suiteId);
    }

    internal static OfficialAuditSuite GetOfficialV2Suite<TSuite>()
    {
        var suiteId = GetOfficialV2SuiteAttribute<TSuite>().Id;

        return s_officialV2Manifest.Value.Suites.Single(suite => suite.Id == suiteId);
    }

    private static OfficialV1Manifest LoadOfficialV1Manifest()
    {
        using var document = LoadManifest(OfficialV1ManifestResource);
        var root = document.RootElement;

        return new OfficialV1Manifest(
            root.GetProperty("revision").GetString()!,
            root.GetProperty("suiteCount").GetInt32(),
            root.GetProperty("caseCount").GetInt32(),
            LoadSuites(root, readV1Sources: true));
    }

    private static OfficialV2Manifest LoadOfficialV2Manifest()
    {
        using var document = LoadManifest(OfficialV2ManifestResource);
        var root = document.RootElement;
        var excludedV1DependentSuites = new List<OfficialV1DependentSuite>();

        foreach (var excludedElement in root
            .GetProperty("excludedV1DependentSuites")
            .EnumerateArray())
        {
            excludedV1DependentSuites.Add(new OfficialV1DependentSuite(
                excludedElement.GetProperty("id").GetString()!,
                excludedElement.GetProperty("caseCount").GetInt32(),
                excludedElement
                    .GetProperty("v1Sources")
                    .EnumerateArray()
                    .Select(source => source.GetString()!)
                    .ToArray()));
        }

        return new OfficialV2Manifest(
            root.GetProperty("revision").GetString()!,
            root.GetProperty("suiteCount").GetInt32(),
            root.GetProperty("caseCount").GetInt32(),
            LoadSuites(root, readV1Sources: false),
            excludedV1DependentSuites);
    }

    private static JsonDocument LoadManifest(string resourceName)
    {
        using var stream = s_assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"Embedded official audit manifest '{resourceName}' was not found.",
                resourceName);

        return JsonDocument.Parse(stream);
    }

    private static IReadOnlyList<OfficialAuditSuite> LoadSuites(
        JsonElement root,
        bool readV1Sources)
    {
        var suites = new List<OfficialAuditSuite>();

        foreach (var suiteElement in root.GetProperty("suites").EnumerateArray())
        {
            var suiteId = suiteElement.GetProperty("id").GetString()!;
            var cases = new List<AuditTestCase>();
            var sources = new List<OfficialSourceSchema>();
            var fixtureModules = new List<OfficialFixtureModule>();
            IReadOnlyList<string> v1Sources = readV1Sources
                ? suiteElement
                    .GetProperty("v1Sources")
                    .EnumerateArray()
                    .Select(source => source.GetString()!)
                    .ToArray()
                : [];

            foreach (var caseElement in suiteElement.GetProperty("cases").EnumerateArray())
            {
                var variables = caseElement.GetProperty("variables");
                var expectedData = caseElement.GetProperty("expectedData");
                var expectsErrors = caseElement.GetProperty("expectsErrors");

                cases.Add(new AuditTestCase(
                    caseElement.GetProperty("id").GetString()!,
                    caseElement.GetProperty("query").GetString()!,
                    variables.ValueKind is JsonValueKind.Null
                        ? null
                        : variables.GetRawText(),
                    caseElement.GetProperty("hasExpectedData").GetBoolean(),
                    expectedData.ValueKind is JsonValueKind.Null
                        ? null
                        : expectedData.GetRawText(),
                    caseElement.GetProperty("hasExpectedErrors").GetBoolean(),
                    expectsErrors.ValueKind is JsonValueKind.Null
                        ? null
                        : expectsErrors.GetBoolean()));
            }

            foreach (var sourceElement in suiteElement.GetProperty("sources").EnumerateArray())
            {
                sources.Add(new OfficialSourceSchema(
                    sourceElement.GetProperty("name").GetString()!,
                    sourceElement.GetProperty("rawSdl").GetString()!,
                    sourceElement.GetProperty("serviceSdl").GetString()!,
                    sourceElement.TryGetProperty("settings", out var settings)
                        ? settings.GetRawText()
                        : null));
            }

            foreach (var moduleElement in suiteElement
                .GetProperty("fixtureModules")
                .EnumerateArray())
            {
                fixtureModules.Add(new OfficialFixtureModule(
                    moduleElement.GetProperty("path").GetString()!,
                    moduleElement.GetProperty("sha256").GetString()!,
                    moduleElement.GetProperty("source").GetString()!));
            }

            suites.Add(new OfficialAuditSuite(
                suiteId,
                cases,
                sources,
                fixtureModules,
                v1Sources));
        }

        return suites;
    }
}

internal sealed record OfficialV1Manifest(
    string Revision,
    int SuiteCount,
    int CaseCount,
    IReadOnlyList<OfficialAuditSuite> Suites);

internal sealed record OfficialV2Manifest(
    string Revision,
    int SuiteCount,
    int CaseCount,
    IReadOnlyList<OfficialAuditSuite> Suites,
    IReadOnlyList<OfficialV1DependentSuite> ExcludedV1DependentSuites);

internal sealed record OfficialAuditSuite(
    string Id,
    IReadOnlyList<AuditTestCase> Cases,
    IReadOnlyList<OfficialSourceSchema> Sources,
    IReadOnlyList<OfficialFixtureModule> FixtureModules,
    IReadOnlyList<string> V1Sources);

internal sealed record OfficialSourceSchema(
    string Name,
    string RawSdl,
    string ServiceSdl,
    string? Settings);

internal sealed record OfficialFixtureModule(string Path, string Sha256, string Source);

internal sealed record OfficialV1DependentSuite(
    string Id,
    int CaseCount,
    IReadOnlyList<string> V1Sources);
