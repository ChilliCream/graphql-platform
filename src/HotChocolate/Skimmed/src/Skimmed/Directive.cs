namespace HotChocolate.Skimmed;

public sealed class Directive
{
    public Directive(DirectiveType type, IReadOnlyList<Argument> arguments)
    {
        Type = type;
        Arguments = arguments;
    }

    public string Name => Type.Name;

    public DirectiveType Type { get; }

    public IReadOnlyList<Argument> Arguments { get; }
}
