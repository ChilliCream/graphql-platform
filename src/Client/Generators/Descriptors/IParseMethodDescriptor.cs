using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public interface IParseMethodDescriptor
        : ICodeDescriptor
    {
        IType ResultType { get; }

        IInterfaceDescriptor ResultDescriptor { get; }
    }

    public interface IParseTypeDescriptor
    {
        IClassDescriptor ResultDescriptor { get; }
    }

}
