namespace HotChocolate.Skimmed;

public interface IHasDirectives : ITypeSystemMember
{
    DirectiveCollection Directives { get; }
}
