namespace HotChocolate.Skimmed;

public interface INamedTypeSystemMember<out TSelf> : IHasName
{
    static new abstract TSelf Create(string name);
}
