using System;
using System.Globalization;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Data.Sorting;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Types
{
    public static class SortObjectFieldDescriptorExtensions
    {
        private static readonly MethodInfo _factoryTemplate =
            typeof(SortObjectFieldDescriptorExtensions)
                .GetMethod(nameof(CreateMiddleware), BindingFlags.Static | BindingFlags.NonPublic)!;

        /// <summary>
        /// Registers the middleware and adds the arguments for sorting
        /// </summary>
        /// <param name="descriptor">The field descriptor where the arguments and middleware are
        /// applied to</param>
        /// <param name="scope">Specifies what scope should be used for the
        /// <see cref="SortConvention" /></param>
        public static IObjectFieldDescriptor UseSorting(
            this IObjectFieldDescriptor descriptor,
            string? scope = null)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return UseSorting(descriptor, null, null, scope);
        }

        /// <summary>
        /// Registers the middleware and adds the arguments for sorting
        /// </summary>
        /// <param name="descriptor">The field descriptor where the arguments and middleware are
        /// applied to</param>
        /// <param name="scope">Specifies what scope should be used for the
        /// <see cref="SortConvention" /></param>
        /// <typeparam name="T">Either a runtime type or a <see cref="SortInputType"/></typeparam>
        public static IObjectFieldDescriptor UseSorting<T>(
            this IObjectFieldDescriptor descriptor,
            string? scope = null)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            Type sortType =
                typeof(ISortInputType).IsAssignableFrom(typeof(T))
                    ? typeof(T)
                    : typeof(SortInputType<>).MakeGenericType(typeof(T));

            return UseSorting(descriptor, sortType, null, scope);
        }

        /// <summary>
        /// Registers the middleware and adds the arguments for sorting
        /// </summary>
        /// <param name="descriptor">The field descriptor where the arguments and middleware are
        /// applied to</param>
        /// <param name="configure">Configures the filter input types that is used by the field
        /// </param>
        /// <param name="scope">Specifies what scope should be used for the
        /// <see cref="SortConvention" /></param>
        public static IObjectFieldDescriptor UseSorting<T>(
            this IObjectFieldDescriptor descriptor,
            Action<ISortInputTypeDescriptor<T>> configure,
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

            var sortType = new SortInputType<T>(configure);
            return UseSorting(descriptor, sortType.GetType(), sortType, scope);
        }

        /// <summary>
        /// Registers the middleware and adds the arguments for sorting
        /// </summary>
        /// <param name="descriptor">The field descriptor where the arguments and middleware are
        /// applied to</param>
        /// <param name="type">Either a runtime type or a <see cref="SortInputType"/></param>
        /// <param name="scope">Specifies what scope should be used for the
        /// <see cref="SortConvention" /></param>
        public static IObjectFieldDescriptor UseSorting(
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

            Type sortType =
                typeof(ISortInputType).IsAssignableFrom(type)
                    ? type
                    : typeof(SortInputType<>).MakeGenericType(type);

            return UseSorting(descriptor, sortType, null, scope);
        }

        private static IObjectFieldDescriptor UseSorting(
            IObjectFieldDescriptor descriptor,
            Type? sortType,
            ITypeSystemMember? sortTypeInstance,
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
                        Type? argumentType = sortType;

                        if (argumentType is null)
                        {
                            if (definition.ResultType is null ||
                                definition.ResultType == typeof(object) ||
                                !c.TypeInspector.TryCreateTypeInfo(
                                    definition.ResultType,
                                    out ITypeInfo? typeInfo))
                            {
                                throw new ArgumentException(
                                    SortObjectFieldDescriptorExtensions_UseSorting_CannotHandleType,
                                    nameof(descriptor));
                            }

                            argumentType = typeof(SortInputType<>)
                                .MakeGenericType(typeInfo.NamedType);
                        }

                        ITypeReference argumentTypeReference = sortTypeInstance is null
                            ? (ITypeReference)c.TypeInspector.GetTypeRef(
                                argumentType,
                                TypeContext.Input,
                                scope)
                            : TypeReference.Create(sortTypeInstance, scope);

                        if (argumentType == typeof(object))
                        {
                            throw SortObjectFieldDescriptorExtensions_CannotInfer();
                        }

                        argumentType = typeof(ListType<>).MakeGenericType(argumentType);

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
                                    (context, def) =>
                                        CompileMiddleware(
                                            context,
                                            def,
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
                                    (context, _) =>
                                        argumentDefinition.Name =
                                            context.GetSortConvention(scope).GetArgumentName())
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
            ISortInputType type = context.GetType<ISortInputType>(argumentTypeReference);
            ISortConvention convention = context.DescriptorContext.GetSortConvention(scope);

            var fieldDescriptor = ObjectFieldDescriptor.From(context.DescriptorContext, definition);
            convention.ConfigureField(fieldDescriptor);

            MethodInfo factory = _factoryTemplate.MakeGenericMethod(type.EntityType.Source);
            var middleware = (FieldMiddleware)factory.Invoke(null, new object[] { convention })!;
            var index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }

        private static FieldMiddleware CreateMiddleware<TEntity>(
            ISortConvention convention) =>
            convention.CreateExecutor<TEntity>();
    }
}
