using System.Collections.Generic;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public interface ITypeModel
    {
        INamedType Type { get; }
    }

    public interface IParserModel
    {
        IReadOnlyList<ComplexOutputTypeModel> Types { get; }
    }
}
