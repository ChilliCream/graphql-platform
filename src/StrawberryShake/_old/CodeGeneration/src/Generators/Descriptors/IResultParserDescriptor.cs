using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IResultParserDescriptor
        : ICodeDescriptor
        , IHasNamespace
    {
        OperationDefinitionNode Operation { get; }

        ICodeDescriptor ResultDescriptor { get; }

        IReadOnlyList<IResultParserMethodDescriptor> ParseMethods { get; }

        IReadOnlyList<INamedType> InvolvedLeafTypes { get; }
    }
}
