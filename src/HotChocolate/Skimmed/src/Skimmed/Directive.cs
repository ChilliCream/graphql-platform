using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class Directive : ITypeSystemMember
{
    public Directive(DirectiveType type, params Argument[] arguments)
        : this(type, (IReadOnlyList<Argument>)arguments)
    {
    }

    public Directive(DirectiveType type, IReadOnlyList<Argument> arguments)
    {
        Type = type;
        Arguments = new(arguments);
    }

    public string Name => Type.Name;

    public DirectiveType Type { get; }

    public ArgumentCollection Arguments { get; }

    public override string ToString()
        => RewriteDirective(this).ToString(true);
}
