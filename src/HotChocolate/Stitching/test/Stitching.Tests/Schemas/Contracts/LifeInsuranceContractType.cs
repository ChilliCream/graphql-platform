using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Contracts;

public class LifeInsuranceContractType : ObjectType<LifeInsuranceContract>
{
    protected override void Configure(
        IObjectTypeDescriptor<LifeInsuranceContract> descriptor)
    {
        descriptor
            .ImplementsNode()
            .IdField(t => t.Id)
            .ResolveNode((ctx, id) =>
            {
                return Task.FromResult(
                    ctx.Service<ContractStorage>()
                        .Contracts
                        .OfType<LifeInsuranceContract>()
                        .FirstOrDefault(t => t.Id.Equals(id)));
            });

        descriptor.Implements<ContractType>();
        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        descriptor.Field(t => t.CustomerId).Type<NonNullType<IdType>>();
        descriptor.Field("foo")
            .Argument("bar", a => a.Type<StringType>())
            .Resolve(ctx => ctx.ArgumentValue<string>("bar"));
        descriptor.Field("error")
            .Type<StringType>()
            .Resolve(ctx => ErrorBuilder.New()
                .SetMessage("Error_Message")
                .SetCode("ERROR_CODE")
                .SetPath(ctx.Path)
                .Build());
        descriptor.Field("date_field")
            .Type<DateType>()
            .Resolve(new DateTime(2018, 5, 17));
        descriptor.Field("date_time_field")
            .Type<DateTimeType>()
            .Resolve(new DateTime(
                2018, 5, 17, 8, 59, 0,
                DateTimeKind.Utc));
        descriptor.Field("string_field")
            .Type<StringType>()
            .Resolve("abc");
        descriptor.Field("id_field")
            .Type<IdType>()
            .Resolve("abc_123");
        descriptor.Field("byte_field")
            .Type<ByteType>()
            .Resolve(123);
        descriptor.Field("int_field")
            .Type<IntType>()
            .Resolve(123);
        descriptor.Field("long_field")
            .Type<LongType>()
            .Resolve(123);
        descriptor.Field("float_field")
            .Type<FloatType>()
            .Argument("f", a => a.Type<FloatType>())
            .Resolve(ctx => ctx.ArgumentValue<double?>("f") ?? 123.123);
        descriptor.Field("decimal_field")
            .Type<DecimalType>()
            .Resolve(123.123);
    }
}
