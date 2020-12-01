using System.Threading.Tasks;

namespace StrawberryShake.Generators.Utilities
{
    public static class CodeWriterExtensions
    {
        private static readonly string _version =
            typeof(CodeWriter).Assembly.GetName().Version!.ToString();

        public static Task WriteGeneratedAttributeAsync(
            this CodeWriter writer)
        {
            return writer.WriteIndentedLineAsync(
                $"[System.CodeDom.Compiler.GeneratedCode(\"StrawberryShake\", \"11.0.0\")]");
        }
    }
}
