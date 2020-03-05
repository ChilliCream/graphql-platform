using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public interface ITypeModel
    {
        INamedType Type { get; }
    }
}
