using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public interface IDocumentModel
    {
        IReadOnlyList<ITypeModel> Types { get; }
    }

    public interface ITypeModel
    {
        INamedType Type { get; }
    }

    public interface IComplexOutputTypeModel : ITypeModel
    {
        string Description { get; }

        SelectionSetNode SelectionSet { get; }

        IReadOnlyList<IComplexOutputTypeModel> Types { get; }

        IReadOnlyList<IOutputFieldModel> Fields { get; }
    }

    public interface IFieldModel
    {
        string Name { get; }

        string? Description { get; }

        IField Field { get; }

        IType Type { get; }
    }

    public interface IOutputFieldModel : IFieldModel
    {
        new IOutputField Field { get; }

        FieldNode Selection { get; }

        Path Path { get; }
    }
}
