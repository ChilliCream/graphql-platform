using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public interface ICompositeSchemaContext
{
    ICompositeType GetType(ITypeNode type);

    DirectiveDefinition GetDirectiveDefinition(string name);
}
