using System;
using System.Globalization;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Types
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

            return UseFiltering(descriptor, null, null, scope);
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

            return UseFiltering(descriptor, filterType, null, scope);
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
            return UseFiltering(descriptor, filterType.GetType(), filterType, scope);
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

            return UseFiltering(descriptor, filterType, null, scope);
        }

        private static IObjectFieldDescriptor UseFiltering(
            IObjectFieldDescriptor descriptor,
            Type? filterType,
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
                        IFilterConvention convention = c.GetFilterConvention(scope);
                        ITypeReference argumentTypeReference;

                        if (filterTypeInstance is not null)
                        {
                            argumentTypeReference = TypeReference.Create(filterTypeInstance, scope);
                        }
                        else if (filterType is null)
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

                            argumentTypeReference = convention.GetFieldType(typeInfo.NamedType);
                        }
                        else
                        {
                            argumentTypeReference = c.TypeInspector.GetTypeRef(
                                filterType,
                                TypeContext.Input,
                                scope);
                        }

                        var argumentDefinition = new ArgumentDefinition
                        {
                            Name = argumentPlaceholder, Type = argumentTypeReference
                        };

                        definition.Arguments.Add(argumentDefinition);

                        definition.Configurations.Add(
                            LazyTypeConfigurationBuilder
                                .New<ObjectFieldDefinition>()
                                .Definition(definition)
                                .Configure(
                                    (context, definition) =>
                                        CompileMiddleware(
                                            context,
                                            definition,
                                            argumentTypeReference,
                                            placeholder,
                                            scope))
                                .On(ApplyConfigurationOn.Completion)
                                .DependsOn(argumentTypeReference, true)
                                .Build());

                        argumentDefinition.Configurations.Add(
                            LazyTypeConfigurationBuilder
                                .New<ArgumentDefinition>()
                                .Definition(argumentDefinition)
                                .Configure(
                                    (context, argumentDefinition) =>
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

            var fieldDescriptor = ObjectFieldDescriptor.From(context.DescriptorContext, definition);
            convention.ConfigureField(fieldDescriptor);

            MethodInfo factory = _factoryTemplate.MakeGenericMethod(type.EntityType.Source);
            var middleware = (FieldMiddleware)factory.Invoke(null,
                new object[]
                {
                    convention
                })!;
            var index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }

        private static FieldMiddleware CreateMiddleware<TEntity>(IFilterConvention convention) =>
            convention.CreateExecutor<TEntity>();
    }
}
