using System;
using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    public interface IInterfaceFieldDescriptor
        : IFluent
    {
        IInterfaceFieldDescriptor Name(string name);
        IInterfaceFieldDescriptor Description(string description);
        IInterfaceFieldDescriptor DeprecationReason(string deprecationReason);
        IInterfaceFieldDescriptor Type<TOutputType>()
            where TOutputType : IOutputType;
        IInterfaceFieldDescriptor Argument(string name, Action<IArgumentDescriptor> argument);
    }

}
