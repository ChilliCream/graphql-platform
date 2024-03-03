using System;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters;

public class IdOperationFilterInputType
    : FilterInputType
    , IComparableOperationFilterInputType
{
    public IdOperationFilterInputType(Action<IFilterInputTypeDescriptor> configure)
        : base(configure)
    {
    }

    [ActivatorUtilitiesConstructor]
    public IdOperationFilterInputType()
    {
    }

    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals)
            .Type<IdType>()
            .ID();

        descriptor.Operation(DefaultFilterOperations.NotEquals)
            .Type<IdType>()
            .ID();

        descriptor.Operation(DefaultFilterOperations.In)
            .Type<ListType<IdType>>()
            .ID();

        descriptor.Operation(DefaultFilterOperations.NotIn)
            .Type<ListType<IdType>>()
            .ID();

        descriptor.AllowAnd(false).AllowOr(false);
    }
}
