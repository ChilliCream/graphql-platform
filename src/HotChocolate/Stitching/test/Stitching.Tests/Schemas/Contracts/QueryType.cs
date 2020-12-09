using System;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.GetContract(default))
                .Argument("contractId", a => a.Type<NonNullType<IdType>>())
                .Type<ContractType>();

            descriptor.Field(t => t.GetContracts(default))
                .Argument("customerId", a => a.Type<NonNullType<IdType>>())
                .Type<ListType<NonNullType<ContractType>>>();

            descriptor.Field("extendedScalar")
                .Argument("d", a => a.Type<DateTimeType>())
                .Type<DateTimeType>()
                .Resolver(ctx =>
                {
                    DateTime dateTime = ctx.ArgumentValue<DateTime>("d");
                    return dateTime;
                });
        }
    }
}
