namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class LambdaBuilder : ICode
{
    private bool _block;
    private bool _isAsync;
    private readonly List<string> _arguments = [];
    private ICode? _code;

    public LambdaBuilder AddArgument(string value)
    {
        _arguments.Add(value);
        return this;
    }

    public LambdaBuilder SetCode(ICode code)
    {
        _code = code;
        return this;
    }

    public LambdaBuilder SetBlock(bool block)
    {
        _block = block;
        return this;
    }

    public LambdaBuilder SetAsync(bool value = true)
    {
        _isAsync = value;
        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (_code is null)
        {
            throw new ArgumentNullException(nameof(_code));
        }

        if (_isAsync)
        {
            writer.Write("async ");
        }

        if (_arguments.Count > 1)
        {
            writer.Write('(');
        }

        for (var i = 0; i < _arguments.Count; i++)
        {
            if (i > 0)
            {
                writer.Write(',');
            }

            writer.Write(_arguments[i]);
        }

        if (_arguments.Count > 1)
        {
            writer.Write(')');
        }

        if (_arguments.Count == 0)
        {
            writer.Write("()");
        }

        writer.Write(" => ");

        if (_block)
        {
            writer.WriteLine();
            writer.WriteIndent();
            writer.WriteLeftBrace();
            writer.WriteLine();
            writer.IncreaseIndent();
        }

        _code.Build(writer);

        if (_block)
        {
            writer.DecreaseIndent();
            writer.WriteIndent();
            writer.WriteRightBrace();
        }
    }

    public static LambdaBuilder New() => new();
}
