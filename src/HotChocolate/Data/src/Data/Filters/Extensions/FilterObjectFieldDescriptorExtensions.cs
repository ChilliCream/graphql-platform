using System;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Data
{
    public static class FilterObjectFieldDescriptorExtensions
    {
        private const string _whereArgumentName = "where";
        private static readonly Type _middlewareDefinition =
            typeof(FilterMiddleware<>);

        public static IObjectFieldDescriptor UseFiltering(
            this IObjectFieldDescriptor descriptor,
            string scope = ConventionBase.DefaultScope)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return UseFiltering(descriptor, null, null, scope);
        }

        public static IObjectFieldDescriptor UseFiltering<T>(
            this IObjectFieldDescriptor descriptor,
            string scope = ConventionBase.DefaultScope)
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

        public static IObjectFieldDescriptor UseFiltering<T>(
            this IObjectFieldDescriptor descriptor,
            Action<IFilterInputTypeDescriptor<T>> configure,
            string scope = ConventionBase.DefaultScope)
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

        private static IObjectFieldDescriptor UseFiltering(
            IObjectFieldDescriptor descriptor,
            Type filterType,
            ITypeSystemMember? filterTypeInstance = null,
            string scope = ConventionBase.DefaultScope)
        {
            FieldMiddleware placeholder = next => context => default;

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCreate(definition =>
                {
                    Type argumentType = filterType;

                    if (filterType == null)
                    {
                        if (!TypeInspector.Default.TryCreate(
                            definition.ResultType, out TypeInfo typeInfo))
                        {
                            // TODO : resources
                            throw new ArgumentException(
                                "Cannot handle the specified type.",
                                nameof(descriptor));
                        }

                        argumentType =
                            typeof(FilterInputType<>).MakeGenericType(
                                typeInfo.ClrType);
                    }

                    ITypeReference? argumentTypeReference = filterTypeInstance is null
                        ? (ITypeReference)new ClrTypeReference(
                            argumentType, TypeContext.Input, scope)
                        : new SchemaTypeReference(filterTypeInstance, scope: scope);

                    if (argumentType == typeof(object))
                    {
                        // TODO : resources
                        throw new SchemaException(
                            SchemaErrorBuilder.New()
                                .SetMessage(
                                    "The filter type cannot be " +
                                    "infered from `System.Object`.")
                                .SetCode(ErrorCodes.Filtering.FilterObjectType)
                                .Build());
                    }

                    var argumentDefinition = new ArgumentDefinition();
                    argumentDefinition.Name = _whereArgumentName;
                    argumentDefinition.Type = new ClrTypeReference(
                        argumentType, TypeContext.Input, scope);
                    definition.Arguments.Add(argumentDefinition);

                    ILazyTypeConfiguration lazyConfiguration =
                        LazyTypeConfigurationBuilder
                            .New<ObjectFieldDefinition>()
                            .Definition(definition)
                            .Configure((context, defintion) =>
                                CompileMiddleware(
                                    context,
                                    definition,
                                    argumentTypeReference,
                                    placeholder,
                                    scope))
                            .On(ApplyConfigurationOn.Completion)
                            .DependsOn(argumentTypeReference, true)
                            .Build();
                    definition.Configurations.Add(lazyConfiguration);
                });

            return descriptor;
        }

        private static void CompileMiddleware(
            ITypeCompletionContext context,
            ObjectFieldDefinition definition,
            ITypeReference argumentTypeReference,
            FieldMiddleware placeholder,
            string scope)
        {
            IFilterConvention convention =
                context.DescriptorContext.GetFilterConvention(scope);
            IFilterInputType type = context.GetType<IFilterInputType>(argumentTypeReference);
            Type middlewareType = _middlewareDefinition.MakeGenericType(type.EntityType);
            FieldMiddleware middleware =
                FieldClassMiddlewareFactory.Create(middlewareType,
                    FilterMiddlewareContext.Create(convention));
            int index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }

        public static IObjectFieldDescriptor AddFilterArguments<TFilter>(
            this IObjectFieldDescriptor descriptor)
            where TFilter : class, IInputType, IFilterInputType
        {
            return descriptor.Argument(_whereArgumentName,
                a => a.Type<TFilter>());
        }

        public static IInterfaceFieldDescriptor AddFilterArguments<TFilter>(
            this IInterfaceFieldDescriptor descriptor)
            where TFilter : class, IInputType, IFilterInputType
        {
            return descriptor.Argument(_whereArgumentName,
                a => a.Type<TFilter>());
        }
    }
}