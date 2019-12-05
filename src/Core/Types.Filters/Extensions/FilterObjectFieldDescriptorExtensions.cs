using System;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public static class FilterObjectFieldDescriptorExtensions
    {
        private const string _whereArgumentNamePlaceholder = "placeholder";
        private static readonly Type _middlewareDefinition =
            typeof(QueryableFilterMiddleware<>);

        public static IObjectFieldDescriptor UseFiltering(
            this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return UseFiltering(descriptor, null);
        }

        public static IObjectFieldDescriptor UseFiltering<T>(
            this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            Type filterType =
                typeof(IFilterInputType).IsAssignableFrom(typeof(T))
                    ? typeof(T)
                    : typeof(FilterInputType<>).MakeGenericType(typeof(T));

            return UseFiltering(descriptor, filterType);
        }

        public static IObjectFieldDescriptor UseFiltering<T>(
            this IObjectFieldDescriptor descriptor,
            Action<IFilterInputTypeDescriptor<T>> configure)
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
            return UseFiltering(descriptor, filterType.GetType(), filterType);
        }

        private static IObjectFieldDescriptor UseFiltering(
            IObjectFieldDescriptor descriptor,
            Type filterType,
            ITypeSystemMember filterTypeInstance = null)
        {
            FieldMiddleware placeholder =
                next => context => Task.CompletedTask;

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
                            throw new ArgumentException(
                                FilterResources.FilterObjectFieldDescriptor_InvalidType,
                                nameof(descriptor));
                        }

                        argumentType =
                            typeof(FilterInputType<>).MakeGenericType(
                                typeInfo.ClrType);
                    }

                    ITypeReference argumentTypeReference =
                        filterTypeInstance is null
                            ? (ITypeReference)new ClrTypeReference(
                                argumentType, TypeContext.Input)
                            : new SchemaTypeReference(filterTypeInstance);

                    if (argumentType == typeof(object))
                    {
                        throw new SchemaException(
                            SchemaErrorBuilder.New()
                                .SetMessage(
                                    FilterResources.FilterObjectFieldDescriptor_InvalidType_Msg)
                                .SetCode(ErrorCodes.Filtering.FilterObjectType)
                                .Build());
                    }

                    var argumentDefinition = new ArgumentDefinition
                    {
                        Type = new ClrTypeReference(
                            argumentType, TypeContext.Input)
                    };

                    argumentDefinition.ConfigureArgumentName();
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

        private static void CompileMiddleware(
            ICompletionContext context,
            ObjectFieldDefinition definition,
            ITypeReference argumentTypeReference,
            FieldMiddleware placeholder)
        {
            IFilterNamingConvention convention = context.DescriptorContext.GetFilterNamingConvention();
            IFilterInputType type =
                context.GetType<IFilterInputType>(argumentTypeReference);
            Type middlewareType = _middlewareDefinition
                .MakeGenericType(type.EntityType);
            FieldMiddleware middleware =
                FieldClassMiddlewareFactory.Create(middlewareType,
                    FilterMiddlewareContext.Create(convention.ArgumentName));
            int index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }

        private static IObjectFieldDescriptor AddFilterArguments(
            this IObjectFieldDescriptor descriptor,
            Type filterType)
        {
            return descriptor.Argument(_whereArgumentNamePlaceholder, a =>
                a.Extend().OnBeforeCreate(d =>
                    d.ConfigureArgumentName().Type = new ClrTypeReference(
                        filterType, TypeContext.Input)));
        }

        public static IObjectFieldDescriptor AddFilterArguments<TFilter>(
            this IObjectFieldDescriptor descriptor)
            where TFilter : class, IInputType, IFilterInputType
        {
            return descriptor.Argument(_whereArgumentNamePlaceholder,
                a => a.Type<TFilter>().Extend().ConfigureArgumentName());
        }

        public static IInterfaceFieldDescriptor AddFilterArguments<TFilter>(
            this IInterfaceFieldDescriptor descriptor)
            where TFilter : class, IInputType, IFilterInputType
        {
            return descriptor.Argument(_whereArgumentNamePlaceholder,
                a => a.Type<TFilter>().Extend().ConfigureArgumentName());
        }

        private static IDescriptorExtension<ArgumentDefinition> ConfigureArgumentName(
            this IDescriptorExtension<ArgumentDefinition> descriptor)
        {
            descriptor.OnBeforeCreate(x => x.ConfigureArgumentName());
            return descriptor;
        }

        private static ArgumentDefinition ConfigureArgumentName(
            this ArgumentDefinition definition)
        {
            ILazyTypeConfiguration lazyArgumentConfiguration =
                LazyTypeConfigurationBuilder
                    .New<ArgumentDefinition>()
                    .Definition(definition)
                    .Configure((context, definition) =>
                    {
                        IFilterNamingConvention convention = context.DescriptorContext.GetFilterNamingConvention();
                        definition.Name = convention.ArgumentName;
                    })
                   .On(ApplyConfigurationOn.Completion)
                   .Build();

            definition.Configurations.Add(lazyArgumentConfiguration);
            return definition;
        }
    }
}
