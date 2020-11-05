using System;
using System.Threading.Tasks;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Data.Projections;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public static class SingleOrDefaultObjectFieldDescriptorExtensions
    {
        private static readonly Type _firstMiddleware = typeof(FirstOrDefaultMiddleware<>);
        private static readonly Type _singleMiddleware = typeof(SingleOrDefaultMiddleware<>);

        public static IObjectFieldDescriptor UseFirstOrDefault(
            this IObjectFieldDescriptor descriptor) =>
            ApplyMiddleware(descriptor, SelectionOptions.FirstOrDefault, _firstMiddleware);

        public static IObjectFieldDescriptor UseSingleOrDefault(
            this IObjectFieldDescriptor descriptor) =>
            ApplyMiddleware(descriptor, SelectionOptions.SingleOrDefault, _singleMiddleware);

        private static IObjectFieldDescriptor ApplyMiddleware(
            this IObjectFieldDescriptor descriptor,
            string optionName,
            Type middlewareDefinition)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            FieldMiddleware placeholder = next => context => default;

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCreate(
                    (context, definition) =>
                    {
                        definition.ContextData[optionName] = null;

                        if (definition.ResultType is null ||
                            !context.TypeInspector.TryCreateTypeInfo(
                                definition.ResultType,
                                out ITypeInfo? typeInfo))
                        {
                            Type resultType = definition.ResolverType ?? typeof(object);
                            throw new ArgumentException(
                                $"Cannot handle the specified type `{resultType.FullName}`.",
                                nameof(descriptor));
                        }

                        Type selectionType = typeInfo.NamedType;
                        definition.ResultType = selectionType;

                        ILazyTypeConfiguration lazyConfiguration =
                            LazyTypeConfigurationBuilder
                                .New<ObjectFieldDefinition>()
                                .Definition(definition)
                                .Configure(
                                    (_, __) =>
                                    {
                                        CompileMiddleware(
                                            selectionType,
                                            definition,
                                            placeholder,
                                            middlewareDefinition);
                                    })
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
            Type middlewareDefinition)
        {
            Type middlewareType = middlewareDefinition.MakeGenericType(type);
            FieldMiddleware middleware = FieldClassMiddlewareFactory.Create(middlewareType);
            var index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }
    }
}
