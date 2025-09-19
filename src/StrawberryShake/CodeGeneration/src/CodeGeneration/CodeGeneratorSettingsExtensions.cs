namespace StrawberryShake.CodeGeneration;

public static class CodeGeneratorSettingsExtensions
{
    public static bool IsStoreEnabled(this CSharpSyntaxGeneratorSettings settings) => !settings.NoStore;

    public static bool IsStoreDisabled(this CSharpSyntaxGeneratorSettings settings) => settings.NoStore;
}
