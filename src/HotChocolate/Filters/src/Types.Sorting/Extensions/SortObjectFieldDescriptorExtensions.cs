using System;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Sorting;

namespace HotChocolate.Types
{
    [Obsolete("Use HotChocolate.Data.")]
    public static class SortObjectFieldDescriptorExtensions
    {
        private static readonly Type _middlewareDefinition = typeof(QueryableSortMiddleware<>);

        [Obsolete("Use HotChocolate.Data.")]
        public static IObjectFieldDescriptor UseSorting(
            this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return UseSorting(descriptor, null);
        }

        [Obsolete("Use HotChocolate.Data.")]
        public static IObjectFieldDescriptor UseSorting<T>(
            this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            Type sortType =
                typeof(ISortInputType).IsAssignableFrom(typeof(T))
                    ? typeof(T)
                    : typeof(SortInputType<>).MakeGenericType(typeof(T));

            return UseSorting(descriptor, sortType);
        }

        [Obsolete("Use HotChocolate.Data.")]
        public static IObjectFieldDescriptor UseSorting<T>(
            this IObjectFieldDescriptor descriptor,
            Action<ISortInputTypeDescriptor<T>> configure)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var sortType = new SortInputType<T>(configure);

            return UseSorting(descriptor, sortType.GetType(), sortType);
        }

        [Obsolete("Use HotChocolate.Data.")]
        public static IObjectFieldDescriptor UseSorting(
            this IObjectFieldDescriptor descriptor,
            Type? sortType,
            ITypeSystemMember? sortTypeInstance = null)
        {
            FieldMiddleware placeholder = next => context => default;
            string argumentPlaceholder =
                "_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCreate((c,definition) =>
                {
                    Type argumentType = GetArgumentType(definition, sortType, c.TypeInspector);

                    ITypeReference argumentTypeReference = sortTypeInstance is null
                        ? (ITypeReference)c.TypeInspector.GetTypeRef(
                            argumentType,
                            TypeContext.Input)
                        : TypeReference.Create(sortTypeInstance);

                    var argumentDefinition = new ArgumentDefinition
                    {
                        Name = argumentPlaceholder,
                        Type = c.TypeInspector.GetTypeRef(argumentType, TypeContext.Input)
                    };

                    ILazyTypeConfiguration lazyArgumentConfiguration =
                        LazyTypeConfigurationBuilder
                            .New<ArgumentDefinition>()
                            .Definition(argumentDefinition)
                            .Configure((context, definition) =>
                                {
                                    ISortingNamingConvention convention =
                                        context.DescriptorContext.GetSortingNamingConvention();
                                    definition.Name = convention.ArgumentName;
                                })
                           .On(ApplyConfigurationOn.Completion)
                           .Build();

                    argumentDefinition.Configurations.Add(lazyArgumentConfiguration);
                    definition.Arguments.Add(argumentDefinition);

                    ILazyTypeConfiguration lazyConfiguration =
                        LazyTypeConfigurationBuilder
                            .New<ObjectFieldDefinition>()
                            .Definition(definition)
                            .Configure((context, definition) =>
                                CompileMiddleware(
                                    context,
                                    definition,
                                    argumentTypeReference,
                                    placeholder))
                            .On(ApplyConfigurationOn.Completion)
                            .DependsOn(argumentTypeReference, true)
                            .Build();
                    definition.Configurations.Add(lazyConfiguration);
                });

            return descriptor;
        }

        private static Type GetArgumentType(
            ObjectFieldDefinition definition,
            Type? filterType,
            ITypeInspector typeInspector)
        {
            Type? argumentType = filterType;

            if (argumentType is null)
            {
                if (definition.ResultType is null ||
                    definition.ResultType == typeof(object) ||
                    !typeInspector.TryCreateTypeInfo(
                        definition.ResultType, out ITypeInfo? typeInfo))
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage("Cannot handle the specified type.")
                            .SetExtension("fieldName", definition.Name)
                            .Build());
                }

                argumentType = typeof(SortInputType<>).MakeGenericType(typeInfo.NamedType);
            }

            if (argumentType == typeof(object))
            {
                // TODO : resources
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage(
                            "The sort type cannot be " +
                            "inferred from `System.Object`.")
                        .SetCode(ErrorCodes.Filtering.FilterObjectType)
                        .Build());
            }

            return argumentType;
        }

        private static void CompileMiddleware(
            ITypeCompletionContext context,
            ObjectFieldDefinition definition,
            ITypeReference argumentTypeReference,
            FieldMiddleware placeholder)
        {
            ISortingNamingConvention convention =
                context.DescriptorContext.GetSortingNamingConvention();

            ISortInputType type = context.GetType<ISortInputType>(argumentTypeReference);
            Type middlewareType = _middlewareDefinition.MakeGenericType(type.EntityType);

            FieldMiddleware middleware =
                FieldClassMiddlewareFactory.Create(
                    middlewareType,
                    (
                        typeof(SortMiddlewareContext),
                        SortMiddlewareContext.Create(convention.ArgumentName
                    )));

            int index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }
    }
}
