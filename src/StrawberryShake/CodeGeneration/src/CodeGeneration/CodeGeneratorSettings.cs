namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Settings for the code generation
    /// </summary>
    public class CodeGeneratorSettings
    {
        public CodeGeneratorSettings(bool noStore, bool useRecords)
        {
            NoStore = noStore;
            UseRecords = useRecords;
        }

        /// <summary>
        /// Generates the client without a store
        /// </summary>
        public bool NoStore { get; }

        /// <summary>
        /// Generates the input types as records
        /// </summary>
        public bool UseRecords { get; }
    }
}
