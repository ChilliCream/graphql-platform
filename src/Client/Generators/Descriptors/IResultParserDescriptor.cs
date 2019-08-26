using System.Collections.Generic;
using HotChocolate.Language;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IResultParserDescriptor
        : ICodeDescriptor
    {
        OperationDefinitionNode Operation { get; }

        IReadOnlyList<IResultParserMethodDescriptor> ParseMethods { get; }
    }
}
