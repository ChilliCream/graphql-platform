using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Analyzers.Types;

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

            var types = new List<ITypeModel>();

            CollectEnumTypes(_schema, _documents, types);
            CollectInputObjectTypes(_schema, _documents, types);


            throw new NotImplementedException();
        }

        private static void CollectEnumTypes(
            ISchema schema,
            IReadOnlyList<DocumentNode> documents,
            ICollection<ITypeModel> types)
        {
            var analyzer = new EnumTypeUsageAnalyzer(schema);

            foreach (DocumentNode document in documents)
            {
                analyzer.Analyze(document);
            }

            foreach (EnumType enumType in analyzer.EnumTypes)
            {
                RenameDirective? rename;
                var values = new List<EnumValueModel>();

                foreach (EnumValue enumValue in enumType.Values)
                {
                    rename = enumValue.Directives.SingleOrDefault<RenameDirective>();

                    EnumValueDirective? value =
                        enumValue.Directives.SingleOrDefault<EnumValueDirective>();

                    values.Add(new EnumValueModel(
                        Utilities.NameUtils.GetClassName(rename?.Name ?? enumValue.Name),
                        enumValue,
                        enumValue.Description,
                        value?.Value));
                }

                rename = enumType.Directives.SingleOrDefault<RenameDirective>();

                SerializationTypeDirective? serializationType =
                    enumType.Directives.SingleOrDefault<SerializationTypeDirective>();

                types.Add(new EnumTypeModel(
                    Utilities.NameUtils.GetClassName(rename?.Name ?? enumType.Name),
                    enumType.Description,
                    enumType,
                    serializationType?.Name,
                    values));
            }
        }

        private void CollectInputObjectTypes(
            ISchema schema,
            IReadOnlyList<DocumentNode> documents,
            ICollection<ITypeModel> types)
        {
            var analyzer = new InputObjectTypeUsageAnalyzer(schema);

            foreach (DocumentNode document in documents)
            {
                analyzer.Analyze(document);
            }

            foreach (InputObjectType inputObjectType in analyzer.InputObjectTypes)
            {
                RenameDirective? rename;
                var fields = new List<InputFieldModel>();

                foreach (IInputField inputField in inputObjectType.Fields)
                {
                    rename = inputField.Directives.SingleOrDefault<RenameDirective>();

                    fields.Add(new InputFieldModel(
                        Utilities.NameUtils.GetClassName(rename?.Name ?? inputField.Name),
                        inputField.Description,
                        inputField,
                        inputField.Type));
                }

                rename = inputObjectType.Directives.SingleOrDefault<RenameDirective>();

                types.Add(new ComplexInputTypeModel(
                    Utilities.NameUtils.GetClassName(rename?.Name ?? inputObjectType.Name),
                    inputObjectType.Description,
                    inputObjectType,
                    fields));
            }
        }
    }
}
