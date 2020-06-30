using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Selections;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public static class SelectionObjectFieldDescriptorExtensions
    {
        private static readonly Type _middlewareDefinition = typeof(SelectionMiddleware<>);

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
            Type objectType)
        {
            FieldMiddleware placeholder =
                next => context => Task.CompletedTask;

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCreate(definition =>
                {
                    Type selectionType = objectType;

                    if (objectType == null)
                    {
                        if (!TypeInspector.Default.TryCreate(
                            definition.ResultType, out TypeInfo typeInfo))
                        {
                            // TODO : resources
                            throw new ArgumentException(
                                "Cannot handle the specified type.",
                                nameof(descriptor));
                        }

                        selectionType = typeInfo.ClrType;
                    }

                    ILazyTypeConfiguration lazyConfiguration =
                        LazyTypeConfigurationBuilder
                            .New<ObjectFieldDefinition>()
                            .Definition(definition)
                            .Configure((context, defintion) =>
                                CompileMiddleware(
                                    selectionType,
                                    definition,
                                    placeholder))
                            .On(ApplyConfigurationOn.Completion)
                            .Build();
                    definition.Configurations.Add(lazyConfiguration);
                });

            return descriptor;
        }

        private static void CompileMiddleware(
            Type type,
            ObjectFieldDefinition definition,
            FieldMiddleware placeholder)
        {
            Type middlewareType = _middlewareDefinition.MakeGenericType(type);
            FieldMiddleware middleware = FieldClassMiddlewareFactory.Create(middlewareType);
            int index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }
    }
}
