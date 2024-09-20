namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class XmlCommentBuilder : ICodeBuilder
{
    private string? _summary;
    private List<string> _code = [];

    public XmlCommentBuilder SetSummary(string summary)
    {
        _summary = summary;
        return this;
    }

    public XmlCommentBuilder AddCode(string code)
    {
        _code.Add(code);
        return this;
    }

    public static XmlCommentBuilder New() => new();

    public void Build(CodeWriter writer)
    {
        if (_summary is not null)
        {
            writer.WriteIndentedLine("/// <summary>");
            WriteCommentLines(writer, _summary);

            foreach (var code in _code)
            {
                writer.WriteIndentedLine("/// <code>");
                WriteCommentLines(writer, code);
                writer.WriteIndentedLine("/// </code>");
            }

            writer.WriteIndentedLine("/// </summary>");
        }
    }

    private void WriteCommentLines(CodeWriter writer, string str)
    {
        using var reader = new StringReader(str);
        var line = reader.ReadLine();
        do
        {
            if (line is not null)
            {
                writer.WriteIndent();
                writer.Write("/// ");
                writer.Write(line);
                writer.WriteLine();
            }

            line = reader.ReadLine();
        }
        while (line is not null);
    }
}
