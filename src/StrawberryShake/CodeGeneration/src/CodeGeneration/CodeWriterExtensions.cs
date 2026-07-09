namespace StrawberryShake.CodeGeneration;

public static class CodeWriterExtensions
{
    public static void WriteGeneratedAttribute(this CodeWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

#if DEBUG
        writer.WriteIndentedLine(
            "[global::System.CodeDom.Compiler.GeneratedCode("
            + "\"StrawberryShake\", \"11.0.0\")]");
#else
        var version = typeof(CodeWriter).Assembly.GetName().Version!.ToString();

        writer.WriteIndentedLine(
            "[global::System.CodeDom.Compiler.GeneratedCode("
            + $"\"StrawberryShake\", \"{version}\")]");
#endif
    }

    public static CodeWriter WriteComment(this CodeWriter writer, string comment)
    {
        writer.Write("// ");
        writer.WriteLine(comment);
        return writer;
    }
}
