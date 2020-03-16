using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Selections;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.DotNetTypeInfoFactory;

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

            FieldMiddleware placeholder = next => context => Task.CompletedTask;

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCreate(definition =>
                {
                    definition.ContextData[optionName] = null;

                    if (!TypeInspector.Default.TryCreate(
                        definition.ResultType, out TypeInfo typeInfo))
                    {
                        Type resultType = definition.ResolverType ?? typeof(object);
                        // TODO : resources
                        throw new ArgumentException(
                            $"Cannot handle the specified type `{resultType.FullName}`.",
                            nameof(descriptor));
                    }

                    Type selectionType = typeInfo.ClrType;
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
            int index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }

        private static ITypeReference RewriteToNonNullableType(ITypeReference type)
        {
            if (type is IClrTypeReference clrTypeRef)
            {
                Type rewritten = Unwrap(UnwrapNonNull(Unwrap(clrTypeRef.Type)));

                rewritten = IsListType(rewritten) ?
                    GetInnerListType(rewritten) :
                    clrTypeRef.Type;

                if (rewritten is null)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage(
                               "The specified type `{0}` is not valid for SingleOrDefault.",
                                clrTypeRef.Type.ToString())
                            .Build());
                }

                return new ClrTypeReference(rewritten, TypeContext.Output);
            }

            throw new NotSupportedException();
        }
    }
}
