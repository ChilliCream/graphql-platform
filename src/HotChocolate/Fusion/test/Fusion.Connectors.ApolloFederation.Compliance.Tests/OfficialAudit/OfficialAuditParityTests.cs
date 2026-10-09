using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Fusion;

public sealed class OfficialAuditParityTests
{
    [Fact]
    [Trait("Category", "OfficialV1Parity")]
    public void V1Manifest_Should_DefineExactInventory_When_Loaded()
    {
        using var manifestStream = typeof(OfficialAuditParityTests).Assembly.GetManifestResourceStream(
            "HotChocolate.Fusion.OfficialAudit.v1-manifest.json")
            ?? throw new InvalidOperationException("The official V1 manifest resource was not found.");
        Assert.Equal(
            "0be75d74affd9ae17520458e82678aa43afe042b7fc849191902db946a0094e2",
            Convert.ToHexString(SHA256.HashData(manifestStream)).ToLowerInvariant());

        var manifest = AuditFixture.GetOfficialV1Manifest();
        var caseIds = manifest.Suites.SelectMany(suite => suite.Cases).Select(c => c.Id).ToArray();

        Assert.Equal("f59c05e3f48f4a4f8e8a731d67a1b71a9788a96f", manifest.Revision);
        Assert.Equal(4, manifest.SuiteCount);
        Assert.Equal(27, manifest.CaseCount);
        Assert.Equal(manifest.SuiteCount, manifest.Suites.Count);
        Assert.Equal(manifest.CaseCount, caseIds.Length);
        Assert.Equal(caseIds.Length, caseIds.Distinct(StringComparer.Ordinal).Count());

        foreach (var suite in manifest.Suites)
        {
            Assert.Equal(
                Enumerable.Range(0, suite.Cases.Count)
                    .Select(index => $"{suite.Id}/{index:000}"),
                suite.Cases.Select(testCase => testCase.Id));
            Assert.Equal(
                suite.Sources.Count,
                suite.Sources.Select(source => source.Name).Distinct(StringComparer.Ordinal).Count());

            foreach (var source in suite.Sources)
            {
                Assert.False(string.IsNullOrWhiteSpace(source.RawSdl));
                Assert.False(string.IsNullOrWhiteSpace(source.ServiceSdl));
                AssertV1SourceSettings(
                    source,
                    suite.V1Sources.Contains(source.Name, StringComparer.Ordinal));
            }

            foreach (var testCase in suite.Cases)
            {
                if (!testCase.HasExpectedData)
                {
                    Assert.Null(testCase.ExpectedData);
                }

                Assert.Equal(testCase.HasExpectedErrors, testCase.ExpectsErrors is not null);
            }

            foreach (var module in suite.FixtureModules)
            {
                var hash = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(module.Source)))
                    .ToLowerInvariant();

                Assert.Equal(module.Sha256, hash);
            }
        }

        Assert.Collection(
            manifest.Suites,
            suite => AssertV1Suite(suite, "abstract-types", 18, "users"),
            suite => AssertV1Suite(suite, "fed1-external-extends", 4, "a", "b"),
            suite => AssertV1Suite(suite, "fed1-external-extends-resolvable", 1, "a", "b"),
            suite => AssertV1Suite(suite, "fed1-external-extension", 4, "a", "b"));
    }

    [Fact]
    [Trait("Category", "OfficialV1Parity")]
    public void V1Port_Should_CoverEverySuiteExactlyOnce_When_Discovered()
    {
        var manifest = AuditFixture.GetOfficialV1Manifest();
        var suites = typeof(OfficialAuditParityTests)
            .Assembly
            .GetTypes()
            .Select(type => new
            {
                Type = type,
                Suite = type.GetCustomAttribute<OfficialV1SuiteAttribute>()
            })
            .Where(item => item.Suite is not null)
            .ToArray();

        Assert.Equal(suites.Length, suites.Select(item => item.Suite!.Id).Distinct().Count());
        Assert.Equal(
            manifest.Suites.Select(suite => suite.Id).Order(StringComparer.Ordinal),
            suites.Select(item => item.Suite!.Id).Order(StringComparer.Ordinal));

        foreach (var item in suites)
        {
            var officialTestMethods = item.Type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes(inherit: false)
                    .Any(attribute => attribute is FactAttribute))
                .ToArray();

            var testMethod = Assert.Single(officialTestMethods);
            Assert.IsType<TheoryAttribute>(
                testMethod.GetCustomAttributes(inherit: false)
                    .Single(attribute => attribute is FactAttribute));
        }
    }

    [Fact]
    [Trait("Category", "OfficialV2Parity")]
    public void Manifest_Should_DefineExactInventory_When_Loaded()
    {
        using var manifestStream = typeof(OfficialAuditParityTests).Assembly.GetManifestResourceStream(
            "HotChocolate.Fusion.OfficialAudit.v2-manifest.json")
            ?? throw new InvalidOperationException("The official V2 manifest resource was not found.");
        Assert.Equal(
            "146ef9f96f2f78ec35b613b67376b2b1741d41946e5f32efd4d0dc9062e3ae29",
            Convert.ToHexString(SHA256.HashData(manifestStream)).ToLowerInvariant());

        var manifest = AuditFixture.GetOfficialV2Manifest();
        var caseIds = manifest.Suites.SelectMany(suite => suite.Cases).Select(c => c.Id).ToArray();

        Assert.Equal("f59c05e3f48f4a4f8e8a731d67a1b71a9788a96f", manifest.Revision);
        Assert.Equal(42, manifest.SuiteCount);
        Assert.Equal(172, manifest.CaseCount);
        Assert.Equal(manifest.SuiteCount, manifest.Suites.Count);
        Assert.Equal(manifest.CaseCount, caseIds.Length);
        Assert.Equal(caseIds.Length, caseIds.Distinct(StringComparer.Ordinal).Count());

        foreach (var suite in manifest.Suites)
        {
            Assert.Equal(
                Enumerable.Range(0, suite.Cases.Count)
                    .Select(index => $"{suite.Id}/{index:000}"),
                suite.Cases.Select(testCase => testCase.Id));
            Assert.Equal(
                suite.Sources.Count,
                suite.Sources.Select(source => source.Name).Distinct(StringComparer.Ordinal).Count());

            foreach (var source in suite.Sources)
            {
                Assert.False(string.IsNullOrWhiteSpace(source.RawSdl));
                Assert.False(string.IsNullOrWhiteSpace(source.ServiceSdl));
            }

            foreach (var testCase in suite.Cases)
            {
                if (!testCase.HasExpectedData)
                {
                    Assert.Null(testCase.ExpectedData);
                }

                Assert.Equal(testCase.HasExpectedErrors, testCase.ExpectsErrors is not null);
            }

            foreach (var module in suite.FixtureModules)
            {
                var hash = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(module.Source)))
                    .ToLowerInvariant();

                Assert.Equal(module.Sha256, hash);
            }
        }

        Assert.Collection(
            manifest.ExcludedV1DependentSuites,
            suite => AssertV1Suite(suite, "abstract-types", 18, "users"),
            suite => AssertV1Suite(suite, "fed1-external-extends", 4, "a", "b"),
            suite => AssertV1Suite(suite, "fed1-external-extends-resolvable", 1, "a", "b"),
            suite => AssertV1Suite(suite, "fed1-external-extension", 4, "a", "b"));
        Assert.Equal(27, manifest.ExcludedV1DependentSuites.Sum(suite => suite.CaseCount));
    }

    [Fact]
    [Trait("Category", "OfficialV2Parity")]
    public void Port_Should_CoverEverySuiteExactlyOnce_When_Discovered()
    {
        var manifest = AuditFixture.GetOfficialV2Manifest();
        var suiteIds = typeof(OfficialAuditParityTests)
            .Assembly
            .GetTypes()
            .Select(type => new
            {
                Type = type,
                Suite = type.GetCustomAttribute<OfficialV2SuiteAttribute>()
            })
            .Where(item => item.Suite is not null)
            .ToArray();

        Assert.Equal(suiteIds.Length, suiteIds.Select(item => item.Suite!.Id).Distinct().Count());
        Assert.Equal(
            manifest.Suites.Select(suite => suite.Id).Order(StringComparer.Ordinal),
            suiteIds.Select(item => item.Suite!.Id).Order(StringComparer.Ordinal));

        foreach (var item in suiteIds)
        {
            var officialTestMethods = item.Type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes(inherit: false)
                    .Any(attribute => attribute is FactAttribute))
                .ToArray();

            var testMethod = Assert.Single(officialTestMethods);
            Assert.IsType<TheoryAttribute>(
                testMethod.GetCustomAttributes(inherit: false)
                    .Single(attribute => attribute is FactAttribute));
        }
    }

    private static void AssertV1Suite(
        OfficialV1DependentSuite suite,
        string id,
        int caseCount,
        params string[] sources)
    {
        Assert.Equal(id, suite.Id);
        Assert.Equal(caseCount, suite.CaseCount);
        Assert.Equal(sources, suite.V1Sources);
    }

    private static void AssertV1Suite(
        OfficialAuditSuite suite,
        string id,
        int caseCount,
        params string[] sources)
    {
        Assert.Equal(id, suite.Id);
        Assert.Equal(caseCount, suite.Cases.Count);
        Assert.Equal(sources, suite.V1Sources);
    }

    private static void AssertV1SourceSettings(
        OfficialSourceSchema source,
        bool isFederationV1)
    {
        using var document = JsonDocument.Parse(source.Settings!);
        var root = document.RootElement;

        if (!isFederationV1)
        {
            Assert.Collection(
                root.EnumerateObject(),
                property =>
                {
                    Assert.Equal("name", property.Name);
                    Assert.Equal(source.Name, property.Value.GetString());
                });
            return;
        }

        Assert.Equal(source.Name, root.GetProperty("name").GetString());
        var extensions = root.GetProperty("extensions");
        Assert.Equal(2, root.EnumerateObject().Count());
        Assert.Single(extensions.EnumerateObject());
        var chillicream = extensions.GetProperty("chillicream");
        Assert.Single(chillicream.EnumerateObject());
        var support = chillicream.GetProperty("apolloFederationSupport");
        Assert.Single(support.EnumerateObject());
        Assert.Equal("1.0", support.GetProperty("version").GetString());
    }
}
