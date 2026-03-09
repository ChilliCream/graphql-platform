namespace HotChocolate.Types.Mutable;

public interface INamedTypeSystemMemberDefinition<out TSelf> : INameProvider
{
    static abstract TSelf Create(string name);
}
