using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal class HasDirectiveMock : Skimmed.IHasDirectives
{
    public HasDirectiveMock(IReadOnlyList<Directive> directives)
    {
        foreach (var directive in directives)
        {
            Directives.Add(directive);
        }   
    }
    
    public DirectiveCollection Directives { get; } = new();
}