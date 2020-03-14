using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.DotNetTypeInfoFactory;

namespace HotChocolate.Types.Selections
{
    public static class SingleOrDefaultObjectFieldDescriptorExtensions
    {
        private static readonly Type _middlewareDefinition = typeof(SingleOrDefaultMiddleware<>);

        private static FieldDelegate Placeholder(FieldDelegate _) => __ => Task.CompletedTask;

        public static IObjectFieldDescriptor UseSingleOrDefault(
            this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor
                .Use(Placeholder)
                .Extend()
                .OnBeforeCreate(definition =>
                {
                    definition.ContextData["__SingleOrDefaultMiddleware"] = true;

                    if (!TypeInspector.Default.TryCreate(
                        definition.ResultType, out TypeInfo typeInfo))
                    {
                        // TODO : resources
                        throw new ArgumentException(
                            "Cannot handle the specified type.",
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
                                    Placeholder);
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
            FieldMiddleware placeholder)
        {
            Type middlewareType = _middlewareDefinition.MakeGenericType(type);
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
