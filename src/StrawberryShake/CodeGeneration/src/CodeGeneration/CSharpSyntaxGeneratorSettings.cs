namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Settings for the syntax generation.
    /// </summary>
    public class CSharpSyntaxGeneratorSettings
    {
        /// <summary>
        /// Creates a new code generator settings instance.
        /// </summary>
        public CSharpSyntaxGeneratorSettings(bool noStore, bool inputRecords)
        {
            NoStore = noStore;
            InputRecords = inputRecords;
        }

        /// <summary>
        /// Generates the client without a store
        /// </summary>
        public bool NoStore { get; }

        /// <summary>
        /// Generates input types as records.
        /// </summary>
        public bool InputRecords { get; }
    }
}
