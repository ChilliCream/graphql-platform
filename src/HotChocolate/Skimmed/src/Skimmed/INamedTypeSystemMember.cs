namespace HotChocolate.Skimmed;

public interface INamedTypeSystemMember<out TSelf> : IHasName
{
    static abstract TSelf Create(string name);
}
