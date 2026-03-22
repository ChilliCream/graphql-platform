using System.Reflection;

namespace Mocha.Analyzers.Tests;

/// <summary>
/// Tests <c>ModuleNameHelper</c> edge cases. Because <c>ModuleNameHelper</c> is internal
/// to <c>Mocha.Analyzers</c> and the test project references it only as an analyzer,
/// we use two strategies:
/// 1. Reflection for direct unit tests of CreateModuleName / SanitizeIdentifier.
/// 2. Snapshot tests through the generator for end-to-end verification.
/// </summary>
public class ModuleNameHelperTests
{
    private static readonly Type s_helperType =
        typeof(MediatorGenerator).Assembly
            .GetType("Mocha.Analyzers.Utils.ModuleNameHelper", throwOnError: true)!;

    private static readonly MethodInfo s_createModuleName =
        s_helperType.GetMethod("CreateModuleName", BindingFlags.Public | BindingFlags.Static)!;

    private static readonly MethodInfo s_sanitizeIdentifier =
        s_helperType.GetMethod("SanitizeIdentifier", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static string CreateModuleName(string? assemblyName)
        => (string)s_createModuleName.Invoke(null, [assemblyName])!;

    private static string SanitizeIdentifier(string input)
        => (string)s_sanitizeIdentifier.Invoke(null, [input])!;

    [Fact]
    public void CreateModuleName_NullAssemblyName_ReturnsAssembly()
    {
        Assert.Equal("Assembly", CreateModuleName(null));
    }

    [Fact]
    public void CreateModuleName_SimpleName_ReturnsSameName()
    {
        Assert.Equal("MyApp", CreateModuleName("MyApp"));
    }

    [Fact]
    public void CreateModuleName_DottedName_ReturnsLastSegment()
    {
        Assert.Equal("Billing", CreateModuleName("MyCompany.Services.Billing"));
    }

    [Fact]
    public void CreateModuleName_TrailingDot_HandlesGracefully()
    {
        // "App." splits into ["App", ""], last segment is ""
        // SanitizeIdentifier("") should produce "_"
        var result = CreateModuleName("App.");
        Assert.False(string.IsNullOrEmpty(result));
        Assert.Equal("_", result);
    }

    [Fact]
    public void CreateModuleName_LeadingNumberSingleSegment_Sanitized()
    {
        // "3rdParty" -> SanitizeIdentifier("3rdParty") -> "_3rdParty"
        var result = CreateModuleName("3rdParty");
        Assert.StartsWith("_", result);
    }

    [Fact]
    public void SanitizeIdentifier_EmptyString_ReturnsUnderscore()
    {
        Assert.Equal("_", SanitizeIdentifier(""));
    }

    [Fact]
    public void SanitizeIdentifier_OnlySpecialChars_ReturnsUnderscores()
    {
        // "---" -> each '-' replaced with '_' -> "___"
        // first char '_' is not a letter -> prepend '_' -> "____"
        var result = SanitizeIdentifier("---");
        Assert.False(string.IsNullOrEmpty(result));
        Assert.StartsWith("_", result);
        Assert.Equal("____", result);
    }

    [Fact]
    public void SanitizeIdentifier_StartsWithDigit_PrependsUnderscore()
    {
        var result = SanitizeIdentifier("123");
        Assert.StartsWith("_", result);
        Assert.Equal("_123", result);
    }

    // End-to-end snapshot test: verify the generator uses the module name correctly
    // for an assembly name with special characters.
    [Fact]
    public async Task Generate_AssemblyNameWithHyphen_UsesLastSegmentSanitized_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record PingCommand() : ICommand;

            public class PingHandler : ICommandHandler<PingCommand>
            {
                public ValueTask HandleAsync(PingCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ], assemblyName: "My-Company.Services.Order-Processing").MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_NullAssemblyName_UsesAssemblyDefault_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record PingCommand() : ICommand;

            public class PingHandler : ICommandHandler<PingCommand>
            {
                public ValueTask HandleAsync(PingCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ], assemblyName: null).MatchMarkdownAsync();
    }
}
