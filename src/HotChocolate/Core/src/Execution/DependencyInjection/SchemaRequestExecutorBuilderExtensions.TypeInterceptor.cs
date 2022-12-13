using System;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder TryAddTypeInterceptor(
        this IRequestExecutorBuilder builder,
        TypeInterceptor typeInterceptor)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (typeInterceptor is null)
        {
            throw new ArgumentNullException(nameof(typeInterceptor));
        }

        return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeInterceptor));
    }

    public static IRequestExecutorBuilder TryAddTypeInterceptor(
        this IRequestExecutorBuilder builder,
        Type typeInterceptor)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (typeInterceptor is null)
        {
            throw new ArgumentNullException(nameof(typeInterceptor));
        }

        return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeInterceptor));
    }

    public static IRequestExecutorBuilder TryAddTypeInterceptor<T>(
        this IRequestExecutorBuilder builder)
        where T : TypeInterceptor
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeof(T)));
    }

    public static IRequestExecutorBuilder TryAddSchemaInterceptor(
        this IRequestExecutorBuilder builder,
        ISchemaInterceptor typeInterceptor)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (typeInterceptor is null)
        {
            throw new ArgumentNullException(nameof(typeInterceptor));
        }

        return builder.ConfigureSchema(b => b.TryAddSchemaInterceptor(typeInterceptor));
    }

    public static IRequestExecutorBuilder TryAddSchemaInterceptor(
        this IRequestExecutorBuilder builder,
        Type typeInterceptor)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (typeInterceptor is null)
        {
            throw new ArgumentNullException(nameof(typeInterceptor));
        }

        return builder.ConfigureSchema(b => b.TryAddSchemaInterceptor(typeInterceptor));
    }

    public static IRequestExecutorBuilder TryAddSchemaInterceptor<T>(
        this IRequestExecutorBuilder builder)
        where T : ISchemaInterceptor
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(b => b.TryAddSchemaInterceptor<T>());
    }

    public static IRequestExecutorBuilder OnBeforeSchemaCreate(
        this IRequestExecutorBuilder builder,
        OnBeforeSchemaCreate onBeforeCreate)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (onBeforeCreate is null)
        {
            throw new ArgumentNullException(nameof(onBeforeCreate));
        }

        return builder.ConfigureSchema(
            b => b.TryAddSchemaInterceptor(
                new DelegateSchemaInterceptor(onBeforeCreate: onBeforeCreate)));
    }

    public static IRequestExecutorBuilder OnAfterSchemaCreate(
        this IRequestExecutorBuilder builder,
        OnAfterSchemaCreate onAfterCreate)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (onAfterCreate is null)
        {
            throw new ArgumentNullException(nameof(onAfterCreate));
        }

        return builder.ConfigureSchema(
            b => b.TryAddSchemaInterceptor(
                new DelegateSchemaInterceptor(onAfterCreate: onAfterCreate)));
    }

    public static IRequestExecutorBuilder OnSchemaError(
        this IRequestExecutorBuilder builder,
        OnSchemaError onError)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (onError is null)
        {
            throw new ArgumentNullException(nameof(onError));
        }

        return builder.ConfigureSchema(
            b => b.TryAddSchemaInterceptor(
                new DelegateSchemaInterceptor(onError: onError)));
    }
}
