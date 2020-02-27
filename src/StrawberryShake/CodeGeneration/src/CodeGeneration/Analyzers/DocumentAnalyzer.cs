using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public class DocumentAnalyzer
    {
        public DocumentAnalyzer SetSchema(ISchema schema)
        {
            return this;
        }

        public DocumentAnalyzer AddDocument(DocumentNode document)
        {
            return this;
        }

        public IDocumentModel Analyze()
        {
            throw new NotImplementedException();
        }
    }

    public interface IDocumentModel
    {
        IReadOnlyList<ITypeModel> Types { get; }
    }

    public interface ITypeModel
    {
        INamedType Type { get; }
    }

    public interface IEnumTypeModel
        : ITypeModel
    {
        string Description { get; }

        string UnderlyingType { get; }

        IReadOnlyList<IEnumValueModel> Values { get; }
    }

    public interface IEnumValueModel
    {
        string Name { get; }

        string Value { get; }

        string Description { get; }

        string UnderlyingValue { get; }
    }

    public interface IComplexOutputTypeModel : ITypeModel
    {
        string Description { get; }

        IReadOnlyList<IComplexOutputTypeModel> Types { get; }

        IReadOnlyList<IOutputFieldModel> Fields { get; }
    }

    public interface IComplexInputTypeModel : ITypeModel
    {
        string Description { get; }

        IReadOnlyList<IInputFieldModel> Fields { get; }
    }

    public interface IFieldModel
    {
        string Name { get; }

        string Description { get; }

        Path Path { get; }

        IField Field { get; }

        FieldNode Selection { get; }

        IType Type { get; }
    }

    public interface IOutputFieldModel : IFieldModel
    {
        new IOutputField Field { get; }
    }

    public interface IInputFieldModel : IFieldModel
    {
        new IOutputField Field { get; }
    }
}
