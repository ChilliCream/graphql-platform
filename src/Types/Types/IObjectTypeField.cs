using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IObjectTypeField
       : IOutputField
    {
        FieldResolverDelegate Resolver { get; }
    }
}
