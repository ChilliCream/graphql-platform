using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public interface IFusionFieldDefinition : IFieldDefinition
{
    bool IsInaccessible { get; }
}
