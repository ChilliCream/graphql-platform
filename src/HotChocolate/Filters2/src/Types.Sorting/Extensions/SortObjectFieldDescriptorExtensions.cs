using System;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Sorting;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public static class SortObjectFieldDescriptorExtensions
    {
        private static readonly Type _middlewareDefinition = typeof(QueryableSortMiddleware<>);

        public static IObjectFieldDescriptor UseSorting(
            this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return UseSorting(descriptor, null);
        }

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

        public static IObjectFieldDescriptor UseSorting(
            this IObjectFieldDescriptor descriptor,
            Type? sortType,
            ITypeSystemMember? sortTypeInstance = null)
        {
            FieldMiddleware placeholder = next => context => default;

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCreate(definition =>
                {
                    Type argumentType = GetArgumentType(definition, sortType);

                    ITypeReference argumentTypeReference =
                        sortTypeInstance is null
                            ? (ITypeReference)TypeReference.Create(
                                argumentType, TypeContext.Input)
                            : new SchemaTypeReference(sortTypeInstance);

                    var argumentDefinition = new ArgumentDefinition
                    {
                        Type = TypeReference.Create(
                            argumentType, TypeContext.Input)
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
            Type? filterType)
        {
            Type? argumentType = filterType;

            if (argumentType == null)
            {
                if (!TypeInspector.Default.TryCreate(
                    definition.ResultType, out TypeInfo typeInfo))
                {
                    // TODO : resources
                    throw new ArgumentException(
                        "Cannot handle the specified type.",
                        definition.ResultType.FullName);
                }

                argumentType =
                    typeof(SortInputType<>).MakeGenericType(
                        typeInfo.ClrType);
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
                    SortMiddlewareContext.Create(convention.ArgumentName));

            int index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }
    }
}
