using HotChocolate.Data.Projections;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types;

public static class SingleOrDefaultObjectFieldDescriptorExtensions
{
    private static readonly Type s_firstMiddleware = typeof(FirstOrDefaultMiddleware<>);
    private static readonly Type s_singleMiddleware = typeof(SingleOrDefaultMiddleware<>);

    public static IObjectFieldDescriptor UseFirstOrDefault(
        this IObjectFieldDescriptor descriptor) =>
        ApplyMiddleware(descriptor, SelectionFlags.FirstOrDefault, s_firstMiddleware);

    public static IObjectFieldDescriptor UseSingleOrDefault(
        this IObjectFieldDescriptor descriptor) =>
        ApplyMiddleware(descriptor, SelectionFlags.SingleOrDefault, s_singleMiddleware);

    private static IObjectFieldDescriptor ApplyMiddleware(
        this IObjectFieldDescriptor descriptor,
        SelectionFlags selectionFlags,
        Type middlewareDefinition)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        FieldMiddlewareConfiguration placeholder =
            new(_ => _ => default, key: WellKnownMiddleware.SingleOrDefault);

        descriptor.Extend().Configuration.MiddlewareConfigurations.Add(placeholder);

        descriptor
            .Extend()
            .OnBeforeCreate(
                (context, definition) =>
                {
                    definition.AddSelectionFlags(selectionFlags | SelectionFlags.MemberIsList);

                    if (definition.ResultType is null
                        || !context.TypeInspector.TryCreateTypeInfo(
                            definition.ResultType,
                            out var typeInfo))
                    {
                        var resultType = definition.ResolverType ?? typeof(object);
                        throw new ArgumentException(
                            $"Cannot handle the specified type `{resultType.FullName}`.",
                            nameof(descriptor));
                    }

                    var selectionType = typeInfo.NamedType;
                    definition.ResultType = selectionType;
                    definition.Type =
                        context.TypeInspector.GetTypeRef(selectionType, TypeContext.Output);

                    definition.Tasks.Add(
                        new OnCompleteTypeSystemConfigurationTask<ObjectFieldConfiguration>(
                            (_, d) =>
                            {
                                CompileMiddleware(
                                    selectionType,
                                    d,
                                    placeholder,
                                    middlewareDefinition);
                            },
                            definition,
                            ApplyConfigurationOn.BeforeCompletion));
                });

        return descriptor;
    }

    private static void CompileMiddleware(
        Type type,
        ObjectFieldConfiguration definition,
        FieldMiddlewareConfiguration placeholder,
        Type middlewareDefinition)
    {
        var middlewareType = middlewareDefinition.MakeGenericType(type);
        var middleware = FieldClassMiddlewareFactory.Create(middlewareType);
        var index = definition.MiddlewareConfigurations.IndexOf(placeholder);
        definition.MiddlewareConfigurations[index] =
            new(middleware, key: WellKnownMiddleware.SingleOrDefault);
    }
}
