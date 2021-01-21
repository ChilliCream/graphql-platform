using System;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration
{
    public static class CodeWriterExtensions
    {
        // TODO : private static readonly string _version =
        //    typeof(CodeWriter).Assembly.GetName().Version!.ToString();

        public static void WriteGeneratedAttribute(this CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.WriteIndentedLine(
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"StrawberryShake\", \"11.0.0\")]");
        }
    }
}
