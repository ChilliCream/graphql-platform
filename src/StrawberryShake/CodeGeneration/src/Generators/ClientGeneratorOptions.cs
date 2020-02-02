namespace StrawberryShake.Generators
{
    public class ClientGeneratorOptions
    {
        private AccessModifier _clientAccessModifier = AccessModifier.Public;
        private AccessModifier _modelAccessModifier = AccessModifier.Public;

        public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp_8_0;

        public bool EnableDISupport { get; set; } = true;

        public AccessModifier ClientAccessModifier
        {
            get => _clientAccessModifier;
            set
            {
                if (value == AccessModifier.Public)
                {
                    _modelAccessModifier = value;
                }
                _clientAccessModifier = value;
            }
        }

        public AccessModifier ModelAccessModifier
        {
            get => _modelAccessModifier;
            set
            {
                if (value == AccessModifier.Internal)
                {
                    _clientAccessModifier = value;
                }
                _modelAccessModifier = value;
            }
        }
    }
}
