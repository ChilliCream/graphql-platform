using System.Diagnostics.CodeAnalysis;
using GreenDonut;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Fetching.Utilities.ThrowHelper;
using static HotChocolate.WellKnownMiddleware;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public static class DataLoaderObjectFieldExtensions
{
    public static IObjectFieldDescriptor UseDataLoader<TDataLoader>(
        this IObjectFieldDescriptor descriptor)
        where TDataLoader : IDataLoader
        => UseDataLoader(descriptor, typeof(TDataLoader));

    public static IObjectFieldDescriptor UseDataLoader(
        this IObjectFieldDescriptor descriptor,
        Type dataLoaderType)
    {
        FieldMiddlewareDefinition placeholder = new(_ => _ => default, key: DataLoader);

        if (!TryGetDataLoaderTypes(dataLoaderType, out var keyType, out var valueType))
        {
            throw DataLoader_InvalidType(dataLoaderType);
        }

        descriptor.Extend().Definition.MiddlewareDefinitions.Add(placeholder);

        descriptor
            .Extend()
            .OnBeforeCreate(
                (c, definition) =>
                {
                    IExtendedType schemaType;
                    if (!valueType.IsArray)
                    {
                        var resolverType =
                            c.TypeInspector.GetType(definition.ResultType!);

                        schemaType = c.TypeInspector.GetType(resolverType.IsArrayOrList
                            ? typeof(IEnumerable<>).MakeGenericType(valueType)
                            : valueType);
                    }
                    else
                    {
                        schemaType = c.TypeInspector.GetType(valueType);
                    }

                    definition.Type = TypeReference.Create(schemaType, TypeContext.Output);
                    definition.Configurations.Add(
                        new CompleteConfiguration<ObjectFieldDefinition>(
                            (_, def) =>
                            {
                                CompileMiddleware(
                                    def,
                                    placeholder,
                                    keyType,
                                    valueType,
                                    dataLoaderType);
                            },
                            definition,
                            ApplyConfigurationOn.BeforeCompletion));
                });

        return descriptor;
    }

    private static void CompileMiddleware(
        ObjectFieldDefinition definition,
        FieldMiddlewareDefinition placeholder,
        Type keyType,
        Type valueType,
        Type dataLoaderType)
    {
        Type middlewareType;
        if (valueType.IsArray)
        {
            middlewareType =
                typeof(GroupedDataLoaderMiddleware<,,>)
                    .MakeGenericType(dataLoaderType, keyType, valueType.GetElementType()!);
        }
        else
        {
            middlewareType =
                typeof(DataLoaderMiddleware<,,>)
                    .MakeGenericType(dataLoaderType, keyType, valueType);
        }

        var middleware = FieldClassMiddlewareFactory.Create(middlewareType);
        var index = definition.MiddlewareDefinitions.IndexOf(placeholder);
        definition.MiddlewareDefinitions[index] = new(middleware, key: DataLoader);
    }

    private static bool TryGetDataLoaderTypes(
        Type type,
        [NotNullWhen(true)] out Type? key,
        [NotNullWhen(true)] out Type? value)
    {
        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType)
            {
                var typeDefinition = interfaceType.GetGenericTypeDefinition();
                if (typeof(IDataLoader<,>) == typeDefinition)
                {
                    key = interfaceType.GetGenericArguments()[0];
                    value = interfaceType.GetGenericArguments()[1];
                    return true;
                }
            }
        }

        key = null;
        value = null;
        return false;
    }

    private sealed class GroupedDataLoaderMiddleware<TDataLoader, TKey, TValue>(FieldDelegate next)
        where TKey : notnull
        where TDataLoader : IDataLoader<TKey, TValue[]>
    {
        public async Task InvokeAsync(IMiddlewareContext context)
        {
            var dataLoader = context.DataLoader<TDataLoader>();

            await next(context).ConfigureAwait(false);

            if (context.Result is IReadOnlyCollection<TKey> values)
            {
                var data = await dataLoader
                    .LoadAsync(values, context.RequestAborted)
                    .ConfigureAwait(false);

                var result = new HashSet<object?>();
                for (var m = 0; m < data.Count; m++)
                {
                    var group = data[m];
                    if (group is not null)
                    {
                        for (var n = 0; n < group.Length; n++)
                        {
                            result.Add(group[n]);
                        }
                    }
                }

                context.Result = result;
            }
            else if (context.Result is TKey value)
            {
                context.Result = await dataLoader
                    .LoadAsync(value, context.RequestAborted)
                    .ConfigureAwait(false);
            }
        }
    }

    private sealed class DataLoaderMiddleware<TDataLoader, TKey, TValue>(FieldDelegate next)
        where TKey : notnull
        where TDataLoader : IDataLoader<TKey, TValue>
    {
        private readonly FieldDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            var dataLoader = context.DataLoader<TDataLoader>();

            await _next(context).ConfigureAwait(false);

            if (context.Result is IReadOnlyCollection<TKey> values)
            {
                context.Result = await dataLoader
                    .LoadAsync(values, context.RequestAborted)
                    .ConfigureAwait(false);
            }
            else if (context.Result is TKey value)
            {
                context.Result = await dataLoader
                    .LoadAsync(value, context.RequestAborted)
                    .ConfigureAwait(false);
            }
        }
    }
}
