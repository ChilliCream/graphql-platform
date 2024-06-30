namespace HotChocolate.Skimmed;

public interface INamedTypeSystemMemberDefinition<out TSelf> : INameProvider
{
    static abstract TSelf Create(string name);
}
