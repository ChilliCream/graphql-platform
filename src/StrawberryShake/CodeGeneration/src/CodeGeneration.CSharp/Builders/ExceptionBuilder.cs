namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class ExceptionBuilder : ICode
{
    private readonly MethodCallBuilder _method =
        MethodCallBuilder
            .New()
            .SetPrefix("throw new ");

    public ExceptionBuilder SetException(string exception)
    {
        _method.SetMethodName(exception);
        return this;
    }

    public ExceptionBuilder AddArgument(string value)
    {
        _method.AddArgument(value);
        return this;
    }

    public ExceptionBuilder AddArgument(ICode value)
    {
        _method.AddArgument(value);
        return this;
    }

    public ExceptionBuilder SetDetermineStatement(bool value)
    {
        _method.SetDetermineStatement(value);
        return this;
    }

    public ExceptionBuilder SetWrapArguments(bool value = true)
    {
        _method.SetWrapArguments(value);
        return this;
    }

    public void Build(CodeWriter writer)
    {
        _method.Build(writer);
    }

    public static ExceptionBuilder New() => new();

    public static ExceptionBuilder New(string types) =>
        new ExceptionBuilder().SetException(types);

    public static ExceptionBuilder Inline(string types) =>
        new ExceptionBuilder().SetException(types).SetDetermineStatement(false);
}
