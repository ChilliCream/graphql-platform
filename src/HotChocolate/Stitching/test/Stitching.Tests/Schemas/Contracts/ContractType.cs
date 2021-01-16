using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public class ContractType : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("Contract");
            descriptor.Implements<NodeType>();
            descriptor.Field("id").Type<NonNullType<IdType>>();
            descriptor.Field("customerId").Type<NonNullType<IdType>>();
        }
    }
}
