using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators
{
    internal interface IModelGeneratorContext
    {
        ISchema Schema { get; }

        IQueryDescriptor Query { get; }

        string ClientName { get; }

        string Namespace { get; }

        IReadOnlyCollection<ICodeDescriptor> Descriptors { get; }

        IReadOnlyDictionary<FieldNode, string> FieldTypes { get; }

        NameString GetOrCreateName(
            ISyntaxNode node,
            NameString name);

        NameString GetOrCreateName(
            ISyntaxNode node,
            NameString name,
            ISet<string> skipNames);

        bool TryGetDescriptor<T>(string name, out T? descriptor)
            where T : class, ICodeDescriptor;

        void Register(FieldNode field, ICodeDescriptor descriptor);

        void Register(ICodeDescriptor descriptor, bool update);

        void Register(ICodeDescriptor descriptor);

        PossibleSelections CollectFields(
            INamedOutputType type,
            SelectionSetNode selectionSet,
            Path path);
    }
}
