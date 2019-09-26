namespace StrawberryShake.Generators
{
    public class ClientGeneratorOptions
    {
        public LanguageVersion LanguageVersion { get; set; } =
            LanguageVersion.CSharp_8_0;

        public bool EnableDISupport { get; set; } = true;
    }
}
