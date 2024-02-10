using System;
using HotChocolate.Data.Projections;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

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

        FieldMiddlewareDefinition placeholder =
            new(_ => _ => default, key: WellKnownMiddleware.SingleOrDefault);

        descriptor.Extend().Definition.MiddlewareDefinitions.Add(placeholder);

        descriptor
            .Extend()
            .OnBeforeCreate(
                (context, definition) =>
                {
                    definition.ContextData[optionName] = true;
                    definition.ContextData[SelectionOptions.MemberIsList] = true;

                    if (definition.ResultType is null ||
                        !context.TypeInspector.TryCreateTypeInfo(
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

                    definition.Configurations.Add(
                        new CompleteConfiguration<ObjectFieldDefinition>(
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
        ObjectFieldDefinition definition,
        FieldMiddlewareDefinition placeholder,
        Type middlewareDefinition)
    {
        var middlewareType = middlewareDefinition.MakeGenericType(type);
        var middleware = FieldClassMiddlewareFactory.Create(middlewareType);
        var index = definition.MiddlewareDefinitions.IndexOf(placeholder);
        definition.MiddlewareDefinitions[index] =
            new(middleware, key: WellKnownMiddleware.SingleOrDefault);
    }
}
