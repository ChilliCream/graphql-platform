using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    ///
    /// </summary>
    public interface ICSharpSyntaxGenerator
    {
        /// <summary>
        /// Defines if this code generator can handle the specified descriptor.
        /// </summary>
        /// <param name="descriptor">
        /// The descriptor that shall be executed with this generator.
        /// </param>
        /// <param name="settings">
        /// Settings for the code generation
        /// </param>
        /// <returns></returns>
        bool CanHandle(
            ICodeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings);

        /// <summary>
        /// Generates code for the specified descriptor and writes
        /// the generated code to the specified code writer.
        /// </summary>
        /// <param name="descriptor">
        /// The code descriptor.
        /// </param>
        /// <param name="settings">
        /// Settings for the code generation
        /// </param>
        CSharpSyntaxGeneratorResult Generate(
            ICodeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings);
    }
}
