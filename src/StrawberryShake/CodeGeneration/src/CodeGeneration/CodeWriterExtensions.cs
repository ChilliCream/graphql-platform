namespace StrawberryShake.CodeGeneration;

public static class CodeWriterExtensions
{
    public static void WriteGeneratedAttribute(this CodeWriter writer)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        var version = typeof(CodeWriter).Assembly.GetName().Version!.ToString();

#if DEBUG
        writer.WriteIndentedLine(
            "[global::System.CodeDom.Compiler.GeneratedCode(" +
            "\"StrawberryShake\", \"11.0.0\")]");
#else
            writer.WriteIndentedLine(
                "[global::System.CodeDom.Compiler.GeneratedCode(" +
                $"\"StrawberryShake\", \"{version}\")]");
#endif
    }

    public static CodeWriter WriteComment(this CodeWriter writer, string comment)
    {
        writer.Write("// ");
        writer.WriteLine(comment);
        return writer;
    }
}
