using System;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IFieldDescriptor
    {
        IFieldDescriptor Description(string description);
        IFieldDescriptor DeprecationReason(string deprecationReason);
        IFieldDescriptor Type<TOutputType>()
            where TOutputType : IOutputType;
        IFieldDescriptor Argument(string name, Action<IArgumentDescriptor> argument);
        IFieldDescriptor Resolver(FieldResolverDelegate fieldResolver);
    }
}
