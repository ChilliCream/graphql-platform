using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public interface IFieldModel
    {
        string Name { get; }

        string? Description { get; }

        IField Field { get; }

        IType Type { get; }
    }
}
