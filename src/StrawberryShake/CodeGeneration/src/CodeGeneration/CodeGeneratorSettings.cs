namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Settings for the code generation
    /// </summary>
    public class CodeGeneratorSettings
    {
        public CodeGeneratorSettings(bool noStore)
        {
            NoStore = noStore;
        }

        /// <summary>
        /// Generates the client without a store
        /// </summary>
        public bool NoStore { get; }
    }
}
