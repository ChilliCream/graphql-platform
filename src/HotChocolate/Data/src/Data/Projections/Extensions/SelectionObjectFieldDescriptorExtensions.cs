using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Data.Projections;
using HotChocolate.Execution;
using static HotChocolate.Data.Projections.ProjectionProvider;
using static HotChocolate.Execution.Processing.SelectionOptimizerHelper;

namespace HotChocolate.Types
{
    public static class ProjectionObjectFieldDescriptorExtensions
    {
        private static readonly MethodInfo _factoryTemplate =
            typeof(ProjectionObjectFieldDescriptorExtensions)
                .GetMethod(nameof(CreateMiddleware), BindingFlags.Static | BindingFlags.NonPublic)!;

        public static IObjectFieldDescriptor IsProjected(
            this IObjectFieldDescriptor descriptor,
            bool isProjected = true)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(
                    x => x.ContextData[ProjectionConvention.IsProjectedKey] = isProjected);

            return descriptor;
        }

        public static IObjectFieldDescriptor UseProjection(
            this IObjectFieldDescriptor descriptor,
            string? scope = null)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return UseProjection(descriptor, null, scope);
        }

        public static IObjectFieldDescriptor UseProjection<T>(
            this IObjectFieldDescriptor descriptor,
            string? scope = null)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return UseProjection(descriptor, typeof(T), scope);
        }

        private static IObjectFieldDescriptor UseProjection(
            IObjectFieldDescriptor descriptor,
            Type? objectType,
            string? scope = null)
        {
            FieldMiddleware placeholder = next => context => default;

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCreate(
                    (context, definition) =>
                    {
                        Type? selectionType = objectType;

                        if (selectionType is null)
                        {
                            if (definition.ResultType is null ||
                                !context.TypeInspector.TryCreateTypeInfo(
                                    definition.ResultType,
                                    out ITypeInfo? typeInfo))
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
                                .Configure(
                                    (context, defintion) =>
                                        CompileMiddleware(
                                            selectionType,
                                            definition,
                                            placeholder,
                                            context,
                                            scope))
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
            ITypeCompletionContext context,
            string? scope)
        {
            IProjectionConvention convention =
                context.DescriptorContext.GetProjectionConvention(scope);
            RegisterOptimizer(definition.ContextData, convention.CreateOptimizer());

            definition.ContextData[ProjectionContextIdentifier] = true;

            MethodInfo factory = _factoryTemplate.MakeGenericMethod(type);
            var middleware = (FieldMiddleware)factory.Invoke(null, new object[] { convention })!;
            var index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }

        private static FieldMiddleware CreateMiddleware<TEntity>(
            IProjectionConvention convention) =>
            convention.CreateExecutor<TEntity>();
    }
}
