namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class CatchBlockBuilder : ICode
{
    private string? _exception;
    private string? _exceptionVariable;
    private readonly List<ICode> _code = [];

    public static CatchBlockBuilder New() => new();

    public CatchBlockBuilder AddCode(ICode code)
    {
        _code.Add(code);
        return this;
    }

    public CatchBlockBuilder SetExceptionVariable(string name)
    {
        if (_exception is null)
        {
            _exception = TypeNames.Exception;
        }

        _exceptionVariable = name;
        return this;
    }

    public CatchBlockBuilder SetExceptionType(string typeName)
    {
        _exception = typeName;
        return this;
    }

    public void Build(CodeWriter writer)
    {
        writer.WriteIndent();
        writer.Write($"catch");
        if (_exception is not null)
        {
            writer.Write("(");
            writer.Write(_exception);

            if (_exceptionVariable is not null)
            {
                writer.WriteSpace();
                writer.Write(_exceptionVariable);
            }

            writer.Write(")");
        }
        writer.WriteLine();

        writer.WriteIndentedLine("{");

        using (writer.IncreaseIndent())
        {
            foreach (var code in _code)
            {
                code.Build(writer);
            }
        }

        writer.WriteIndentedLine("}");
    }
}
