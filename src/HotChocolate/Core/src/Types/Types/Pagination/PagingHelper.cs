using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    public static class PagingHelper
    {
        public static IObjectFieldDescriptor UsePaging(
            IObjectFieldDescriptor descriptor,
            Type type,
            Type? entityType = null,
            GetPagingProvider? resolvePagingProvider = null,
            PagingSettings settings = default)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            FieldMiddleware placeholder = next => context => default;

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCompletion((c, d) =>
                {
                    settings = c.GetSettings(settings);
                    entityType ??= c.GetType<IOutputType>(d.Type).ToRuntimeType();
                    resolvePagingProvider ??= ResolvePagingProvider;

                    IExtendedType sourceType = GetSourceType(c.TypeInspector, d, entityType);
                    IPagingProvider pagingProvider = resolvePagingProvider(c.Services, sourceType);
                    IPagingHandler pagingHandler = pagingProvider.CreateHandler(sourceType, settings);
                    FieldMiddleware middleware = CreateMiddleware(pagingHandler);
                    var index = d.MiddlewareComponents.IndexOf(placeholder);
                    d.MiddlewareComponents[index] = middleware;
                })
                .DependsOn(type);

            return descriptor;
        }

        private static IExtendedType GetSourceType(
            ITypeInspector typeInspector,
            ObjectFieldDefinition definition,
            Type entityType)
        {
            // if an explicit result type is defined we will type it since it expresses the
            // intend.
            if (definition.ResultType is not null)
            {
                return typeInspector.GetType(definition.ResultType);
            }

            // Otherwise we will look at specified members and extract the return type.
            MemberInfo? member = definition.ResolverMember ?? definition.Member;
            if (member is not null)
            {
                return typeInspector.GetReturnType(member, true);
            }

            // if we were not able to resolve the source type we will assume that it is
            // an enumerable of the entity type.
            return typeInspector.GetType(typeof(IEnumerable<>).MakeGenericType(entityType));
        }

        private static FieldMiddleware CreateMiddleware(
            IPagingHandler handler) =>
            FieldClassMiddlewareFactory.Create(
                typeof(PagingMiddleware), 
                (typeof(IPagingHandler), handler));

        public static PagingSettings GetSettings(
            this ITypeCompletionContext context,
            PagingSettings settings) =>
            context.DescriptorContext.GetSettings(settings);

        public static PagingSettings GetSettings(
            this IDescriptorContext context,
            PagingSettings settings)
        {
            PagingSettings global = default;
            if (context.ContextData.TryGetValue(typeof(PagingSettings).FullName!, out object? o) &&
                o is PagingSettings casted)
            {
                global = casted;
            }

            return new PagingSettings
            {
                DefaultPageSize = settings.DefaultPageSize ?? global.DefaultPageSize,
                MaxPageSize = settings.MaxPageSize ?? global.MaxPageSize,
                IncludeTotalCount = settings.IncludeTotalCount ?? global.IncludeTotalCount,
            };
        }

        private static IPagingProvider ResolvePagingProvider(
            IServiceProvider services,
            IExtendedType source) =>
            services.GetServices<IPagingProvider>().First(p => p.CanHandle(source));
    }
}
