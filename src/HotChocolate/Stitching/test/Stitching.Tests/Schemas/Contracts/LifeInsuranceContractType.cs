using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public class LifeInsuranceContractType
        : ObjectType<LifeInsuranceContract>
    {
        protected override void Configure(
            IObjectTypeDescriptor<LifeInsuranceContract> descriptor)
        {
            descriptor
                .AsNode()
                .IdField(t => t.Id)
                .NodeResolver((ctx, id) =>
                {
                    return Task.FromResult(
                        ctx.Service<ContractStorage>()
                            .Contracts
                            .OfType<LifeInsuranceContract>()
                            .FirstOrDefault(t => t.Id.Equals(id)));
                });

            descriptor.Interface<ContractType>();
            descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
            descriptor.Field(t => t.CustomerId).Type<NonNullType<IdType>>();
            descriptor.Field("foo")
                .Argument("bar", a => a.Type<StringType>())
                .Resolver(ctx => ctx.Argument<string>("bar"));
            descriptor.Field("error")
                .Type<StringType>()
                .Resolver(ctx => ErrorBuilder.New()
                    .SetMessage("Error_Message")
                    .SetCode("ERROR_CODE")
                    .SetPath(ctx.Path)
                    .Build());
            descriptor.Field("date_field")
                .Type<DateType>()
                .Resolver(new DateTime(2018, 5, 17));
            descriptor.Field("date_time_field")
                .Type<DateTimeType>()
                .Resolver(new DateTime(
                    2018, 5, 17, 8, 59, 0,
                    DateTimeKind.Utc));
            descriptor.Field("string_field")
                .Type<StringType>()
                .Resolver("abc");
            descriptor.Field("id_field")
                .Type<IdType>()
                .Resolver("abc_123");
            descriptor.Field("byte_field")
                .Type<ByteType>()
                .Resolver(123);
            descriptor.Field("int_field")
                .Type<IntType>()
                .Resolver(123);
            descriptor.Field("long_field")
                .Type<LongType>()
                .Resolver(123);
            descriptor.Field("float_field")
                .Type<FloatType>()
                .Resolver(123.123);
            descriptor.Field("decimal_field")
                .Type<DecimalType>()
                .Resolver(123.123);
        }
    }
}
