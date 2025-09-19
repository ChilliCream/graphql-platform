namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class TryCatchBuilder : ICode
{
    private readonly List<ICode> _try = [];
    private readonly List<CatchBlockBuilder> _catch = [];

    public static TryCatchBuilder New() => new();

    public TryCatchBuilder AddTryCode(ICode code)
    {
        _try.Add(code);
        return this;
    }

    public TryCatchBuilder AddCatchBlock(CatchBlockBuilder code)
    {
        _catch.Add(code);
        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (_catch.Count == 0 || _try.Count == 0)
        {
            throw new InvalidOperationException(
                "The catch build needs at least one try and one catch.");
        }

        writer.WriteIndentedLine("try");
        writer.WriteIndentedLine("{");

        using (writer.IncreaseIndent())
        {
            foreach (var code in _try)
            {
                code.Build(writer);
            }
        }

        writer.WriteIndentedLine("}");

        foreach (var code in _catch)
        {
            code.Build(writer);
        }
    }
}
