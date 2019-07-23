using System;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public static class FilterObjectFieldDescriptorExtensions
    {
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

        private static IObjectFieldDescriptor UseFiltering(
            IObjectFieldDescriptor descriptor,
            Type filterType)
        {
            FieldMiddleware placeholder =
                next => context => Task.CompletedTask;

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCreate(definition =>
                {
                    Type argumentType = filterType == null
                        ? typeof(FilterInputType<>).MakeGenericType(
                            definition.ResolverType)
                        : filterType;

                    var argumentTypeReference = new ClrTypeReference(
                        argumentType,
                        TypeContext.Input);

                    if (argumentType == typeof(object))
                    {
                        // TODO : resources
                        throw new SchemaException(
                            SchemaErrorBuilder.New()
                                .SetMessage(
                                    "The filter type cannot be " +
                                    "infered from `System.Object`.")
                                .SetCode("FILTER_OBJECT_TYPE")
                                .Build());
                    }

                    var argumentDefinition = new ArgumentDefinition();
                    argumentDefinition.Name = "where";
                    argumentDefinition.Type = new ClrTypeReference(
                        filterType, TypeContext.Input);
                    definition.Arguments.Add(argumentDefinition);

                    ILazyTypeConfiguration lazyConfiguration =
                        LazyTypeConfigurationBuilder
                            .New<ObjectFieldDefinition>()
                            .Configure((context, defintion) =>
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
            IFilterInputType type =
                context.GetType<IFilterInputType>(argumentTypeReference);
            Type middlewareType = _middlewareDefinition
                .MakeGenericType(type.EntityType);
            FieldMiddleware middleware =
                FieldClassMiddlewareFactory.Create(middlewareType);
            int index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }

        private static IObjectFieldDescriptor AddFilterArguments(
            this IObjectFieldDescriptor descriptor,
            Type filterType)
        {
            return descriptor.Argument("where", a =>
                a.Extend().OnBeforeCreate(d =>
                    d.Type = new ClrTypeReference(
                        filterType, TypeContext.Input)));
        }

        public static IObjectFieldDescriptor AddFilterArguments<TFilter>(
            this IObjectFieldDescriptor descriptor)
            where TFilter : class, IInputType, IFilterInputType
        {
            return descriptor.Argument("where", a => a.Type<TFilter>());
        }

        public static IInterfaceFieldDescriptor AddFilterArguments<TFilter>(
            this IInterfaceFieldDescriptor descriptor)
            where TFilter : class, IInputType, IFilterInputType
        {
            return descriptor.Argument("where", a => a.Type<TFilter>());
        }
    }
}
