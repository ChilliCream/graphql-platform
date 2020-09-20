using System;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Selections;
using HotChocolate.Types.Sorting;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public static class SelectionObjectFieldDescriptorExtensions
    {
        private static readonly Type _middlewareDefinition
            = typeof(SelectionMiddleware<>);

        public static IObjectFieldDescriptor UseSelection(
            this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }
            return UseSelection(descriptor, null);
        }

        public static IObjectFieldDescriptor UseSelection<T>(
            this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }
            return UseSelection(descriptor, typeof(T));
        }

        private static IObjectFieldDescriptor UseSelection(
            IObjectFieldDescriptor descriptor,
            Type? objectType)
        {
            FieldMiddleware placeholder = next => context => default;

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCreate((context, definition) =>
                {
                    Type? selectionType = objectType;

                    if (selectionType is null)
                    {
                        if (definition.ResultType is null ||
                            !context.TypeInspector.TryCreateTypeInfo(
                                definition.ResultType, out ITypeInfo? typeInfo))
                        {
                            throw new ArgumentException(
                                "Cannot handle the specified type.",
                                nameof(descriptor));
                        }

                        selectionType = typeInfo.NamedType;
                    }

                    ILazyTypeConfiguration lazyConfiguration =
                        LazyTypeConfigurationBuilder
                            .New<ObjectFieldDefinition>()
                            .Definition(definition)
                            .Configure((context, defintion) =>
                                CompileMiddleware(
                                    selectionType,
                                    definition,
                                    placeholder,
                                    context))
                            .On(ApplyConfigurationOn.Completion)
                            .Build();
                    definition.Configurations.Add(lazyConfiguration);
                });

            return descriptor;
        }

        private static void CompileMiddleware(
            Type type,
            ObjectFieldDefinition definition,
            FieldMiddleware placeholder,
            ITypeCompletionContext context)
        {
            string filterConventionArgumentName =
                context.DescriptorContext.GetFilterNamingConvention().ArgumentName;
            string sortingConventionArgumentName =
                context.DescriptorContext.GetSortingNamingConvention().ArgumentName;
            Type middlewareType = _middlewareDefinition.MakeGenericType(type);
            FieldMiddleware middleware =
                FieldClassMiddlewareFactory.Create(middlewareType,
                    SelectionMiddlewareContext.Create(
                        filterConventionArgumentName, sortingConventionArgumentName));
            int index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }
    }
}
