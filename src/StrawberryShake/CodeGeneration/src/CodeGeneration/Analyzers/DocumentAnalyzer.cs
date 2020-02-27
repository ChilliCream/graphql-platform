using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public class DocumentAnalyzer
    {
        private ISchema? _schema;
        private readonly List<DocumentNode> _documents = new List<DocumentNode>();

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
            if (_schema is null)
            {
                throw new InvalidOperationException(
                    "You must provide a schema.");
            }

            if (_documents.Count == 0)
            {
                throw new InvalidOperationException(
                    "You must at least provide one document.");
            }



            throw new NotImplementedException();
        }

        private static IReadOnlyList<EnumType> CollectEnumTypes(
            ISchema schema,
            IReadOnlyList<DocumentNode> documents)
        {
            var analyzer = new EnumTypeUsageAnalyzer(schema);

            foreach (DocumentNode document in documents)
            {
                analyzer.Analyze(document);
            }

            return analyzer.EnumTypes.ToArray();
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

    public class EnumTypeModel
        : ITypeModel
    {
        public string Description { get; }

        public string UnderlyingType { get; }

        public IReadOnlyList<EnumValueModel> Values { get; }
    }

    public class EnumValueModel
    {
        public EnumValueModel(
            string name,
            EnumValue value,
            string description,
            string underlyingValue)
        {
            Name = name;
            Value = value;
            Description = description;
            UnderlyingValue = underlyingValue;
        }

        public string Name { get; }

        public EnumValue Value { get; }

        public string Description { get; }

        public string UnderlyingValue { get; }
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
