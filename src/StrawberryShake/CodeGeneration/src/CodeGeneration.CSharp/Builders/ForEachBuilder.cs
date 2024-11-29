namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class ForEachBuilder : ICodeContainer<ForEachBuilder>
{
    private ICode? _loopHeader;
    private readonly List<ICode> _lines = [];

    public static ForEachBuilder New() => new();

    public ForEachBuilder AddCode(string code, bool addIf = true)
    {
        AddCode(
            CodeLineBuilder.New().SetLine(code),
            addIf);
        return this;
    }

    public ForEachBuilder AddCode(ICode code, bool addIf = true)
    {
        if (addIf)
        {
            _lines.Add(code);
        }

        return this;
    }

    public ForEachBuilder AddEmptyLine()
    {
        _lines.Add(CodeLineBuilder.New());
        return this;
    }

    public ForEachBuilder SetLoopHeader(string elementCode)
    {
        _loopHeader = CodeInlineBuilder.New().SetText(elementCode);
        return this;
    }

    public ForEachBuilder SetLoopHeader(ICode elementCode)
    {
        _loopHeader = elementCode;
        return this;
    }

    public void Build(CodeWriter writer)
    {
        writer.WriteIndent();
        writer.Write("foreach (");
        _loopHeader?.Build(writer);
        writer.Write(")");
        writer.WriteLine();
        writer.WriteIndent();
        writer.WriteLine("{");
        using (writer.IncreaseIndent())
        {
            foreach (var line in _lines)
            {
                line.Build(writer);
            }
        }

        writer.WriteIndent();
        writer.WriteLine("}");
    }
}
