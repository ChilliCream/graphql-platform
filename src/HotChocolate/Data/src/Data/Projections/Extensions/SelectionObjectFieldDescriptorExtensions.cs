using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Data.Projections;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using static HotChocolate.Data.Projections.ProjectionProvider;
using static HotChocolate.Execution.Processing.SelectionOptimizerHelper;

namespace HotChocolate.Types
{
    public class ProjectionOptimizer : ISelectionOptimizer
    {
        private readonly IProjectionProvider _convention;

        public ProjectionOptimizer(
            IProjectionProvider convention)
        {
            _convention = convention;
        }

        public void OptimizeSelectionSet(SelectionOptimizerContext context)
        {
            var processedFields = new HashSet<string>();
            while (!processedFields.SetEquals(context.Fields.Keys))
            {
                var fieldsToProcess = new HashSet<string>(context.Fields.Keys);
                fieldsToProcess.ExceptWith(processedFields);
                foreach (var field in fieldsToProcess)
                {
                    context.Fields[field] =
                        _convention.RewriteSelection(context, context.Fields[field]);
                    processedFields.Add(field);
                }
            }
        }

        public bool AllowFragmentDeferral(
            SelectionOptimizerContext context,
            InlineFragmentNode fragment)
        {
            return false;
        }

        public bool AllowFragmentDeferral(
            SelectionOptimizerContext context,
            FragmentSpreadNode fragmentSpread,
            FragmentDefinitionNode fragmentDefinition)
        {
            return false;
        }
    }

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
            IProjectionProvider convention =
                context.DescriptorContext.GetProjectionConvention(scope);
            RegisterOptimizer(definition.ContextData, new ProjectionOptimizer(convention));

            definition.ContextData[ProjectionContextIdentifier] = true;

            MethodInfo factory = _factoryTemplate.MakeGenericMethod(type);
            var middleware = (FieldMiddleware)factory.Invoke(null, new object[] { convention })!;
            var index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }

        private static FieldMiddleware CreateMiddleware<TEntity>(
            IProjectionProvider convention) =>
            convention.CreateExecutor<TEntity>();
    }
}
