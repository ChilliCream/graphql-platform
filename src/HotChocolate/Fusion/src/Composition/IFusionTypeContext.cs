using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public interface IFusionTypeContext
{
    DirectiveType DeclareDirective { get; } 
    
    DirectiveType FusionDirective { get; }

    DirectiveType IsDirective { get; }
    
    DirectiveType NodeDirective { get; }
    
    DirectiveType PrivateDirective { get; }
    
    DirectiveType RemoveDirective { get; }

    DirectiveType RenameDirective { get; }

    DirectiveType RequireDirective { get; }
    
    DirectiveType ResolveDirective { get; }
    
    DirectiveType ResolverDirective { get; }
    
    DirectiveType SourceDirective { get; }

    DirectiveType TransportDirective { get; }

    DirectiveType VariableDirective { get; }
}
