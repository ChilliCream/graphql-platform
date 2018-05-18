using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal interface IResolverBindingContext
    {
        Field LookupField(FieldReference fieldReference);
        string LookupFieldName(FieldResolverMember fieldResolverMember);
    }

}
