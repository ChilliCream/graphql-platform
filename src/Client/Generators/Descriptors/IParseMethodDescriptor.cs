using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public interface IResultParseMethodDescriptor
        : ICodeDescriptor
    {
        IType ResultType { get; }

        FieldNode ResultSelection { get; }

        Path Path { get; }

        IInterfaceDescriptor ResultDescriptor { get; }

        IReadOnlyCollection<IResultParseTypeDescriptor> PossibleTypes { get; }
    }

    public interface IResultParseTypeDescriptor
    {
        IClassDescriptor ResultDescriptor { get; }
    }

}
