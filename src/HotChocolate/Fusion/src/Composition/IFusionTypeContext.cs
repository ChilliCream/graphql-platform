using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public interface IFusionTypeContext
{
    DirectiveType DeclareDirective { get; } 
    
    DirectiveType IsDirective { get; }
    
    DirectiveType RemoveDirective { get; }
    
    DirectiveType RequireDirective { get; }

    DirectiveType RenameDirective { get; }
    
    DirectiveType ResolveDirective { get; }
}