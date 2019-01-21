using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public class QueryType
        : ObjectType<Query>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.GetContract(default))
                .Argument("contractId", a => a.Type<NonNullType<IdType>>())
                .Type<ContractType>();

            descriptor.Field(t => t.GetContract(default))
                .Argument("contractId", a => a.Type<NonNullType<StringType>>())
                .Type<ListType<NonNullType<ContractType>>>();
        }
    }
}
