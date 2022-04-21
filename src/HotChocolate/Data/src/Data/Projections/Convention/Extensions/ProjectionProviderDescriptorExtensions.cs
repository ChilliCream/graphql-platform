using System;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Handlers;

namespace HotChocolate.Data;

public static class ProjectionProviderDescriptorExtensions
{
    public static IProjectionProviderDescriptor AddDefaults<TScalarHandler, TListHandler, TObjectHandler>(this IProjectionProviderDescriptor descriptor)
        where TScalarHandler : IProjectionFieldHandler
        where TListHandler : IProjectionFieldHandler
        where TObjectHandler : IProjectionFieldHandler
    {
        return descriptor.RegisterQueryableHandler<TScalarHandler, TListHandler, TObjectHandler>();
    }

    public static IProjectionProviderDescriptor RegisterQueryableHandler<TScalarHandler, TListHandler, TObjectHandler>(this IProjectionProviderDescriptor descriptor)
        where TScalarHandler : IProjectionFieldHandler
        where TListHandler : IProjectionFieldHandler
        where TObjectHandler : IProjectionFieldHandler
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor.RegisterFieldHandler<TScalarHandler>();
        descriptor.RegisterFieldHandler<TListHandler>();
        descriptor.RegisterFieldHandler<TObjectHandler>();

        descriptor.RegisterFieldInterceptor<QueryableFilterInterceptor>();
        descriptor.RegisterFieldInterceptor<QueryableSortInterceptor>();
        descriptor.RegisterFieldInterceptor<QueryableFirstOrDefaultInterceptor>();
        descriptor.RegisterFieldInterceptor<QueryableSingleOrDefaultInterceptor>();

        descriptor.RegisterOptimizer<IsProjectedProjectionOptimizer>();
        descriptor.RegisterOptimizer<QueryablePagingProjectionOptimizer>();
        descriptor.RegisterOptimizer<QueryableFilterProjectionOptimizer>();
        descriptor.RegisterOptimizer<QueryableSortProjectionOptimizer>();

        return descriptor;
    }
}
