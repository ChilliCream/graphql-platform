namespace HotChocolate.Types.Analyzers.Models;

public readonly struct MemberBinding(string name, MemberBindingKind kind)
{
    public string Name => name;

    public MemberBindingKind Kind => kind;
}
