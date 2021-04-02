using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration
{
    public interface ICodeGenerator
    {
        /// <summary>
        /// Defines if this code generator can handle the specified descriptor.
        /// </summary>
        /// <param name="settings">
        /// Settings for the code generation
        /// </param>
        /// <param name="descriptor">
        /// The descriptor that shall be executed with this generator.
        /// </param>
        /// <returns></returns>
        bool CanHandle(
            CodeGeneratorSettings settings,
            ICodeDescriptor descriptor);

        /// <summary>
        /// Generates code for the specified descriptor and writes
        /// the generated code to the specified code writer.
        /// </summary>
        /// <param name="writer">
        /// The code writer.
        /// </param>
        /// <param name="descriptor">
        /// The code descriptor.
        /// </param>
        /// <param name="settings">
        /// Settings for the code generation
        /// </param>
        /// <param name="fileName">
        /// The name of the file.
        /// </param>
        /// <param name="path">
        /// A hint where this file should be located.
        /// </param>
        void Generate(
            CodeWriter writer,
            ICodeDescriptor descriptor,
            CodeGeneratorSettings settings,
            out string fileName,
            out string? path);
    }
}
