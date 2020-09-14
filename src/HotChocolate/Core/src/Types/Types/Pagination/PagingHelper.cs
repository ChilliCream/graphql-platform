using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    public static class PagingHelper
    {
        public static IObjectFieldDescriptor UsePaging(
            IObjectFieldDescriptor descriptor,
            Type? type,
            Type? entityType = null,
            GetPagingProvider? resolvePagingProvider = null,
            PagingSettings settings = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            FieldMiddleware placeholder = next => context => default;

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCreate((c, d) =>
                {
                    MemberInfo? member = d.ResolverMember ?? d.Member;
                    IExtendedType schemaType = GetSchemaType(c.TypeInspector, member, type);

                    var configuration = new TypeConfiguration<ObjectFieldDefinition>
                    {
                        Definition = d,
                        On = ApplyConfigurationOn.Completion,
                        Configure = (c, d) => ApplyConfiguration(
                            c, d, entityType, resolvePagingProvider, settings, placeholder)
                    };

                    configuration.Dependencies.Add(
                        new TypeDependency(
                            TypeReference.Create(schemaType, TypeContext.Output),
                            TypeDependencyKind.Named));

                    d.Configurations.Add(configuration);
                });

            return descriptor;
        }

        private static void ApplyConfiguration(
            ITypeCompletionContext context,
            ObjectFieldDefinition definition,
            Type? entityType,
            GetPagingProvider? resolvePagingProvider,
            PagingSettings settings,
            FieldMiddleware placeholder)
        {
            settings = context.GetSettings(settings);
            entityType ??= context.GetType<IOutputType>(definition.Type).ToRuntimeType();
            resolvePagingProvider ??= ResolvePagingProvider;

            IExtendedType sourceType = GetSourceType(context.TypeInspector, definition, entityType);
            IPagingProvider pagingProvider = resolvePagingProvider(context.Services, sourceType);
            IPagingHandler pagingHandler = pagingProvider.CreateHandler(sourceType, settings);
            FieldMiddleware middleware = CreateMiddleware(pagingHandler);

            var index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
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

        public static IExtendedType GetSchemaType(
            ITypeInspector typeInspector,
            MemberInfo? member,
            Type? type)
        {
            if (type is null &&
                member is not null &&
                typeInspector.GetOutputReturnTypeRef(member) is ExtendedTypeReference r &&
                typeInspector.TryCreateTypeInfo(r.Type, out ITypeInfo? typeInfo))
            {
                // if the member has already associated a schema type with an attribute for instance
                // we will just take it. Since we want the entity element we are going to take
                // the element type of the list or array as our entity type.
                if (r.Type.IsSchemaType && r.Type.IsArrayOrList)
                {
                    return r.Type.ElementType!;
                }

                // if the member type is unknown we will try to infer it by extracting 
                // the named type component from it and running the type inference. 
                // It might be that we either are unable to infer or get the wrong type 
                // in special cases. In the case we are getting it wrong the user has 
                // to explicitly bind the type.
                if (SchemaTypeResolver.TryInferSchemaType(
                    typeInspector,
                    r.WithType(typeInspector.GetType(typeInfo.NamedType)),
                    out ExtendedTypeReference schemaTypeRef))
                {
                    // if we are able to infer the type we will reconstruct its structure so that
                    // we can correctly extract from it the element type with the correct
                    // nullability information.
                    Type current = schemaTypeRef.Type.Type;

                    foreach (TypeComponent component in typeInfo.Components.Reverse().Skip(1))
                    {
                        if (component.Kind == TypeComponentKind.NonNull)
                        {
                            current = typeof(NonNullType<>).MakeGenericType(current);
                        }
                        else if (component.Kind == TypeComponentKind.List)
                        {
                            current = typeof(ListType<>).MakeGenericType(current);
                        }
                    }

                    if (typeInspector.GetType(current) is { IsArrayOrList: true } schemaType)
                    {
                        return schemaType.ElementType!;
                    }
                }
            }

            if (type is null || !typeof(IType).IsAssignableFrom(type))
            {
                throw ThrowHelper.UsePagingAttribute_NodeTypeUnknown(member);
            }

            return typeInspector.GetType(type);
        }

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
