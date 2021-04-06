namespace StrawberryShake.CodeGeneration
{
    public static class CodeGeneratorSettingsExtensions
    {
        public static bool IsStoreEnabled(this CodeGeneratorSettings settings) => !settings.NoStore;

        public static bool IsStoreDisabled(this CodeGeneratorSettings settings) => settings.NoStore;
    }
}
