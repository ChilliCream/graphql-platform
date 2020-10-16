using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public class SomeOtherContractType
        : ObjectType<SomeOtherContract>
    {
        protected override void Configure(
            IObjectTypeDescriptor<SomeOtherContract> descriptor)
        {
            descriptor
                .AsNode()
                .IdField(t => t.Id)
                .NodeResolver((ctx, id) =>
                {
                    return Task.FromResult(
                        ctx.Service<ContractStorage>()
                            .Contracts
                            .OfType<SomeOtherContract>()
                            .FirstOrDefault(t => t.Id.Equals(id)));
                });

            descriptor.Interface<ContractType>();

            descriptor.Field(t => t.Id)
                .Type<NonNullType<IdType>>();

            descriptor.Field(t => t.CustomerId)
                .Type<NonNullType<IdType>>();

            descriptor.Field(t => t.ExpiryDate)
                .Type<NonNullType<DateTimeType>>();
        }
    }
}
