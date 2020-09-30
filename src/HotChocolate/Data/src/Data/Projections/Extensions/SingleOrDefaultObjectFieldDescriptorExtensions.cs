using System;
using System.Threading.Tasks;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Data.Projections;

namespace HotChocolate.Types
{
    /*
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
                .OnBeforeCreate((context, definition) =>
                {
                    definition.ContextData[optionName] = null;


                    if (definition.ResultType is null ||
                        !context.TypeInspector.TryCreateTypeInfo(
                            definition.ResultType, out ITypeInfo? typeInfo))
                    {
                        Type resultType = definition.ResolverType ?? typeof(object);
                        throw new ArgumentException(
                            $"Cannot handle the specified type `{resultType.FullName}`.",
                            nameof(descriptor));
                    }

                    Type selectionType = typeInfo.NamedType;
                    definition.ResultType = selectionType;
                    definition.Type = RewriteToNonNullableType(definition.Type);

                    ILazyTypeConfiguration lazyConfiguration =
                        LazyTypeConfigurationBuilder
                            .New<ObjectFieldDefinition>()
                            .Definition(definition)
                            .Configure((_, __) =>
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

        private static ITypeReference RewriteToNonNullableType(ITypeReference type)
        {
            if (type is ExtendedTypeReference extendedTypeRef)
            {
                IExtendedType rewritten = Unwrap(extendedTypeRef.Type);
                rewritten = rewritten.ElementType ?? extendedTypeRef.Type;
                return extendedTypeRef.WithType(rewritten);
            }

            throw new NotSupportedException();
        }

        private static IExtendedType Unwrap(IExtendedType type)
        {
            IExtendedType current = type;

            while (IsWrapperType(current) || IsTaskType(current) || IsOptional(current))
            {
                current = type.TypeArguments[0];
            }

            return current;
        }

        private static bool IsWrapperType(IExtendedType type) =>
            type.IsGeneric &&
            typeof(NativeType<>) == type.Definition;

        private static bool IsTaskType(IExtendedType type) =>
            type.IsGeneric &&
            (typeof(Task<>) == type.Definition ||
             typeof(ValueTask<>) == type.Definition);

        private static bool IsOptional(IExtendedType type) =>
            type.IsGeneric &&
            typeof(Optional<>) == type.Definition;
    }
    */
}
