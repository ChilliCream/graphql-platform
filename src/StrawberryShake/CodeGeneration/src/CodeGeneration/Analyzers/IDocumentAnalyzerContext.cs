using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal interface IDocumentAnalyzerContext
    {
        ISchema Schema { get; }

        IReadOnlyCollection<ITypeModel> Types { get; }

        IReadOnlyCollection<OperationModel> Operations { get; }

        IReadOnlyCollection<FieldParserModel> FieldParsers { get; }

        NameString GetOrCreateName(
            ISyntaxNode node,
            NameString name,
            ISet<string>? skipNames = null);

        NameString GetOrCreateName(NameString name);

        SelectionVariants CollectFields(
            INamedOutputType type,
            SelectionSetNode selectionSet,
            Path path);

        IEnumerable<OutputTypeModel> GetTypes(SelectionSetNode selectionSet);

        bool TryGetModel<T>(string name, [NotNullWhen(true)]out T? model)
            where T : class, ITypeModel;

        void SetDocument(DocumentNode document);

        void Register(OutputTypeModel type, bool update = false);

        void Register(InputObjectTypeModel objectType);

        void Register(EnumTypeModel type);

        void Register(OperationModel operation);

        void Register(FieldParserModel parser);
    }
}
