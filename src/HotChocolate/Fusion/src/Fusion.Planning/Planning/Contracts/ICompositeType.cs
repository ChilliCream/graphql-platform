using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public interface ICompositeType
{
    TypeKind Kind { get; }
}

public interface ICompositeNamedType : ICompositeType
{
    string Name { get; }
}
