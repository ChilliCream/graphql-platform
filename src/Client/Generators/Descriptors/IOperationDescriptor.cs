using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Descriptors
{
    public interface IOperationDescriptor
        : ICodeDescriptor
    {
        OperationDefinitionNode Operation { get; }

        ObjectType OperationType { get; }

        IQueryDescriptor Query { get; }

        IReadOnlyList<IArgumentDescriptor> Arguments { get; }

        ICodeDescriptor ResultType { get; }
    }
}
