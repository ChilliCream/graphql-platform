using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data
{
    public static class FilterObjectFieldDescriptorExtensions
    {
        private static readonly MethodInfo _factoryTemplate =
            typeof(FilterObjectFieldDescriptorExtensions)
                .GetMethod(nameof(CreateMiddleware), BindingFlags.Static | BindingFlags.NonPublic)!;

        /// <summary>
        /// Registers the middleware and adds the arguments for filtering
        /// </summary>
        /// <param name="descriptor">The field descriptor where the arguments and middleware are
        /// applied to</param>
        /// <param name="scope">Specifies what scope should be used for the
        /// <see cref="FilterConvention" /></param>
        public static IObjectFieldDescriptor UseFiltering(
            this IObjectFieldDescriptor descriptor,
            string? scope = null)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return UseFiltering(descriptor, null, null, null, scope);
        }

        /// <summary>
        /// Registers the middleware and adds the arguments for filtering
        /// </summary>
        /// <param name="descriptor">The field descriptor where the arguments and middleware are
        /// applied to</param>
        /// <param name="scope">Specifies what scope should be used for the
        /// <see cref="FilterConvention" /></param>
        /// <typeparam name="T">Either a runtime type or a <see cref="FilterInputType"/></typeparam>
        public static IObjectFieldDescriptor UseFiltering<T>(
            this IObjectFieldDescriptor descriptor,
            string? scope = null)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            Type filterType =
                typeof(IFilterInputType).IsAssignableFrom(typeof(T))
                    ? typeof(T)
                    : typeof(FilterInputType<>).MakeGenericType(typeof(T));

            return UseFiltering(descriptor, filterType, null, null, scope);
        }

        /// <summary>
        /// Registers the middleware and adds the arguments for filtering
        /// </summary>
        /// <param name="descriptor">The field descriptor where the arguments and middleware are
        /// applied to</param>
        /// <param name="configure">Configures the filter input types that is used by the field
        /// </param>
        /// <param name="scope">Specifies what scope should be used for the
        /// <see cref="FilterConvention" /></param>
        public static IObjectFieldDescriptor UseFiltering<T>(
            this IObjectFieldDescriptor descriptor,
            Action<IFilterInputTypeDescriptor<T>> configure,
            string? scope = null)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var filterType = new FilterInputType<T>(configure);
            return UseFiltering(descriptor, filterType.GetType(), null, filterType, scope);
        }

        /// <summary>
        /// Registers the middleware and adds the arguments for filtering
        /// </summary>
        /// <param name="descriptor">The field descriptor where the arguments and middleware are
        /// applied to</param>
        /// <param name="type">Either a runtime type or a <see cref="FilterInputType"/></param>
        /// <param name="scope">Specifies what scope should be used for the
        /// <see cref="FilterConvention" /></param>
        public static IObjectFieldDescriptor UseFiltering(
            this IObjectFieldDescriptor descriptor,
            Type type,
            string? scope = null)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type filterType =
                typeof(IFilterInputType).IsAssignableFrom(type)
                    ? type
                    : typeof(FilterInputType<>).MakeGenericType(type);

            return UseFiltering(descriptor, filterType, null, null, scope);
        }

        /// <summary>
        /// Registers the middleware and adds the arguments for filtering
        /// </summary>
        /// <param name="descriptor">The field descriptor where the arguments and middleware are
        /// applied to</param>
        /// <param name="schemaType">Either a runtime type or a <see cref="FilterInputType"/></param>
        /// <param name="entityType">The type that is returned by the data source. schemaType will be used if none is provided</param>
        /// <param name="scope">Specifies what scope should be used for the
        /// <see cref="FilterConvention" /></param>
        public static IObjectFieldDescriptor UseFiltering(
            this IObjectFieldDescriptor descriptor,
            Type schemaType,
            Type? entityType,
            string? scope = null)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (schemaType is null)
            {
                throw new ArgumentNullException(nameof(schemaType));
            }

            Type filterType =
                typeof(IFilterInputType).IsAssignableFrom(schemaType)
                    ? schemaType
                    : typeof(FilterInputType<>).MakeGenericType(schemaType);

            return UseFiltering(descriptor, filterType, entityType, null, scope);
        }

        private static IObjectFieldDescriptor UseFiltering(
            IObjectFieldDescriptor descriptor,
            Type? filterType,
            Type? entityType,
            ITypeSystemMember? filterTypeInstance,
            string? scope)
        {
            FieldMiddleware placeholder = next => context => default;
            string argumentPlaceholder =
                "_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCreate(
                    (c, definition) =>
                    {
                        Type? argumentType = filterType;

                        if (argumentType is null)
                        {
                            if (definition.ResultType is null ||
                                definition.ResultType == typeof(object) ||
                                !c.TypeInspector.TryCreateTypeInfo(
                                    definition.ResultType,
                                    out ITypeInfo? typeInfo))
                            {
                                throw new ArgumentException(
                                    FilterObjectFieldDescriptorExtensions_UseFiltering_CannotHandleType,
                                    nameof(descriptor));
                            }

                            argumentType = typeof(FilterInputType<>)
                                .MakeGenericType(typeInfo.NamedType);
                        }

                        ITypeReference argumentTypeReference = filterTypeInstance is null
                            ? (ITypeReference)c.TypeInspector.GetTypeRef(
                                entityType ?? argumentType,
                                TypeContext.Input,
                                scope)
                            : TypeReference.Create(filterTypeInstance, scope);

                        if (argumentType == typeof(object))
                        {
                            throw FilterObjectFieldDescriptorExtensions_CannotInfer();
                        }

                        var argumentDefinition = new ArgumentDefinition
                        {
                            Name = argumentPlaceholder,
                            Type = c.TypeInspector.GetTypeRef(
                                argumentType,
                                TypeContext.Input,
                                scope)
                        };
                        definition.Arguments.Add(argumentDefinition);

                        definition.Configurations.Add(
                            LazyTypeConfigurationBuilder
                                .New<ObjectFieldDefinition>()
                                .Definition(definition)
                                .Configure(
                                    (context, defintion) =>
                                        CompileMiddleware(
                                            context,
                                            definition,
                                            argumentTypeReference,
                                            placeholder,
                                            scope))
                                .On(ApplyConfigurationOn.Completion)
                                .DependsOn(argumentTypeReference, true)
                                .Build());

                        definition.Configurations.Add(
                            LazyTypeConfigurationBuilder
                                .New<ObjectFieldDefinition>()
                                .Definition(definition)
                                .Configure(
                                    (context, defintion) =>
                                        argumentDefinition.Name =
                                            context.GetFilterConvention(scope).GetArgumentName())
                                .On(ApplyConfigurationOn.Naming)
                                .Build());
                    });

            return descriptor;
        }

        private static void CompileMiddleware(
            ITypeCompletionContext context,
            ObjectFieldDefinition definition,
            ITypeReference argumentTypeReference,
            FieldMiddleware placeholder,
            string? scope)
        {
            IFilterInputType type = context.GetType<IFilterInputType>(argumentTypeReference);
            IFilterConvention convention = context.DescriptorContext.GetFilterConvention(scope);

            MethodInfo factory = _factoryTemplate.MakeGenericMethod(type.EntityType.Source);
            var middleware = (FieldMiddleware)factory.Invoke(null, new object[] { convention })!;
            var index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }

        private static FieldMiddleware CreateMiddleware<TEntity>(
            IFilterConvention convention) =>
            convention.CreateExecutor<TEntity>();
    }
}
