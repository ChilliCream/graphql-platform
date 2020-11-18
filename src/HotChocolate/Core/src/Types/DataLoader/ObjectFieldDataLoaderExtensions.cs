using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types
{
    public static class DataLoaderObjectFieldExtensions
    {
        public static IObjectFieldDescriptor UseDataloader<TDataLoader>(
            this IObjectFieldDescriptor descriptor)
            where TDataLoader : IDataLoader
        {
            return UseDataloader(descriptor, typeof(TDataLoader));
        }

        public static IObjectFieldDescriptor UseDataloader(
            this IObjectFieldDescriptor descriptor,
            Type dataLoaderType)
        {
            FieldMiddleware placeholder = next => context => default;

            if (!TryGetDataLoaderTypes(dataLoaderType, out Type? keyType, out Type? valueType))
            {
                throw DataLoader_InvalidType(dataLoaderType);
            }

            descriptor
                .Use(placeholder)
                .Extend()
                .OnBeforeCreate(
                    (c, definition) =>
                    {
                        IExtendedType schemaType;
                        if (!valueType.IsArray)
                        {
                            IExtendedType resolverType =
                                c.TypeInspector.GetType(definition.ResultType);

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
                            LazyTypeConfigurationBuilder
                                .New<ObjectFieldDefinition>()
                                .Definition(definition)
                                .Configure(
                                    (context, def) =>
                                    {
                                        CompileMiddleware(
                                            def,
                                            placeholder,
                                            keyType,
                                            valueType,
                                            dataLoaderType);
                                    })
                                .On(ApplyConfigurationOn.Completion)
                                .Build());
                    });

            return descriptor;
        }

        private static void CompileMiddleware(
            ObjectFieldDefinition definition,
            FieldMiddleware placeholder,
            Type keyType,
            Type valueType,
            Type dataLoaderType)
        {
            Type middlewareType;
            if (valueType.IsArray)
            {
                middlewareType =
                    typeof(GroupedDataLoaderMiddleware<,,>)
                        .MakeGenericType(dataLoaderType, keyType, valueType.GetElementType());
            }
            else
            {
                middlewareType =
                    typeof(DataLoaderMiddleware<,,>)
                        .MakeGenericType(dataLoaderType, keyType, valueType);
            }

            FieldMiddleware middleware = FieldClassMiddlewareFactory.Create(middlewareType);
            var index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = middleware;
        }

        private static bool TryGetDataLoaderTypes(
            Type type,
            [NotNullWhen(true)] out Type? key,
            [NotNullWhen(true)] out Type? value)
        {
            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType)
                {
                    Type? typeDefinition = interfaceType.GetGenericTypeDefinition();
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

        private class GroupedDataLoaderMiddleware<TDataLoader, TKey, TValue>
            where TKey : notnull
            where TDataLoader : IDataLoader<TKey, TValue[]>
        {
            private readonly FieldDelegate _next;

            public GroupedDataLoaderMiddleware(FieldDelegate next)
            {
                _next = next ?? throw new ArgumentNullException(nameof(next));
            }

            public async Task InvokeAsync(IMiddlewareContext context)
            {
                var dataloader = context.DataLoader<TDataLoader>();

                await _next(context).ConfigureAwait(false);

                if (context.Result is IReadOnlyCollection<TKey> values)
                {
                    IReadOnlyList<TValue[]> data = await dataloader
                        .LoadAsync(values, context.RequestAborted);

                    var result = new HashSet<object>();
                    for (var m = 0; m < data.Count; m++)
                    {
                        for (var n = 0; n < data[m].Length; n++)
                        {
                            result.Add(data[m][n]);
                        }
                    }

                    context.Result = result;
                }
                else if (context.Result is TKey value)
                {
                    context.Result = await dataloader.LoadAsync(value, context.RequestAborted);
                }
            }
        }

        private class DataLoaderMiddleware<TDataLoader, TKey, TValue>
            where TKey : notnull
            where TDataLoader : IDataLoader<TKey, TValue>
        {
            private readonly FieldDelegate _next;

            public DataLoaderMiddleware(FieldDelegate next)
            {
                _next = next ?? throw new ArgumentNullException(nameof(next));
            }

            public async Task InvokeAsync(IMiddlewareContext context)
            {
                var dataloader = context.DataLoader<TDataLoader>();

                await _next(context).ConfigureAwait(false);

                if (context.Result is IReadOnlyCollection<TKey> values)
                {
                    context.Result = await dataloader
                        .LoadAsync(values, context.RequestAborted);
                }
                else if (context.Result is TKey value)
                {
                    context.Result = await dataloader.LoadAsync(value, context.RequestAborted);
                }
            }
        }
    }
}
