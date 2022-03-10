using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using HotChocolate.Resolvers;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate;

public static class ResolverContextExtensions
{
    [Obsolete("Use `GetGlobalStateOrDefault`")]
    [return: MaybeNull]
    public static T GetGlobalValue<T>(
        this IResolverContext context,
        string name)
        => GetGlobalStateOrDefault<T>(context, name);

    [return: MaybeNull]
    public static T GetGlobalStateOrDefault<T>(
        this IResolverContext context,
        string name)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw String_NullOrEmpty(nameof(name));
        }

        if (context.ContextData.TryGetValue(name, out var value) &&
            value is T casted)
        {
            return casted;
        }

        return default;
    }

    [Obsolete("Use `GetScopedStateOrDefault`")]
    [return: MaybeNull]
    public static T GetScopedValue<T>(
        this IResolverContext context,
        string name)
        => GetScopedStateOrDefault<T>(context, name);

    [return: MaybeNull]
    public static T GetScopedStateOrDefault<T>(
        this IResolverContext context,
        string name)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw String_NullOrEmpty(nameof(name));
        }

        if (context.ScopedContextData.TryGetValue(name, out var value) &&
            value is T casted)
        {
            return casted;
        }

        return default;
    }

    [Obsolete("Use `GetLocalStateOrDefault`")]
    [return: MaybeNull]
    public static T GetLocalValue<T>(
        this IResolverContext context,
        string name)
        => GetLocalStateOrDefault<T>(context, name);

    [return: MaybeNull]
    public static T GetLocalStateOrDefault<T>(
        this IResolverContext context,
        string name)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw String_NullOrEmpty(nameof(name));
        }

        if (context.LocalContextData.TryGetValue(name, out var value) &&
            value is T casted)
        {
            return casted;
        }

        return default;
    }

    [Obsolete("Use `SetGlobalState`")]
    public static void SetGlobalValue<T>(
        this IResolverContext context,
        string name,
        [MaybeNull] T value)
        => SetGlobalState(context, name, value);

    public static void SetGlobalState<T>(
        this IResolverContext context,
        string name,
        [MaybeNull] T value)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw String_NullOrEmpty(nameof(name));
        }

        context.ContextData[name] = value;
    }

    [Obsolete("Use `SetScopedState`")]
    public static void SetScopedValue<T>(
        this IResolverContext context,
        string name,
        [MaybeNull] T value)
        => SetScopedState(context, name, value);

    public static void SetScopedState<T>(
        this IResolverContext context,
        string name,
        [MaybeNull] T value)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw String_NullOrEmpty(nameof(name));
        }

        context.ScopedContextData = context.ScopedContextData.SetItem(name, value);
    }

    [Obsolete("Use `SetLocalState`")]
    public static void SetLocalValue<T>(
        this IResolverContext context,
        string name,
        [MaybeNull] T value)
        => SetLocalState(context, name, value);

    public static void SetLocalState<T>(
        this IResolverContext context,
        string name,
        [MaybeNull] T value)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw String_NullOrEmpty(nameof(name));
        }

        context.LocalContextData = context.LocalContextData.SetItem(name, value);
    }

    [Obsolete("Use `GetOrAddGlobalState`")]
    [return: MaybeNull]
    public static T GetOrAddGlobalValue<T>(
        this IResolverContext context,
        string name,
        Func<string, T> createValue)
        => GetOrAddGlobalState(context, name, createValue);

    [return: MaybeNull]
    public static T GetOrAddGlobalState<T>(
        this IResolverContext context,
        string name,
        Func<string, T> createValue)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw String_NullOrEmpty(nameof(name));
        }

        if (createValue is null)
        {
            throw new ArgumentNullException(nameof(createValue));
        }

        if (context.ContextData.TryGetValue(name, out var value) &&
            value is T casted)
        {
            return casted;
        }
        else
        {
            T newValue = createValue(name);
            SetGlobalState(context, name, newValue);
            return newValue;
        }
    }

    [Obsolete("Use `GetOrAddScopedState`")]
    [return: MaybeNull]
    public static T GetOrAddScopedValue<T>(
        this IResolverContext context,
        string name,
        Func<string, T> createValue)
        => GetOrAddScopedState(context, name, createValue);

    [return: MaybeNull]
    public static T GetOrAddScopedState<T>(
        this IResolverContext context,
        string name,
        Func<string, T> createValue)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw String_NullOrEmpty(nameof(name));
        }

        if (createValue is null)
        {
            throw new ArgumentNullException(nameof(createValue));
        }

        if (context.ScopedContextData.TryGetValue(name, out var value) &&
            value is T casted)
        {
            return casted;
        }
        else
        {
            T newValue = createValue(name);
            SetScopedState(context, name, newValue);
            return newValue;
        }
    }

    [Obsolete("Use `GetOrAddLocalState`")]
    [return: MaybeNull]
    public static T GetOrAddLocalValue<T>(
        this IResolverContext context,
        string name,
        Func<string, T> createValue)
        => GetOrAddLocalState(context, name, createValue);

    [return: MaybeNull]
    public static T GetOrAddLocalState<T>(
        this IResolverContext context,
        string name,
        Func<string, T> createValue)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw String_NullOrEmpty(nameof(name));
        }

        if (createValue is null)
        {
            throw new ArgumentNullException(nameof(createValue));
        }

        if (context.LocalContextData.TryGetValue(name, out var value) &&
            value is T casted)
        {
            return casted;
        }
        else
        {
            T newValue = createValue(name);
            SetLocalState(context, name, newValue);
            return newValue;
        }
    }

    [Obsolete("Use `RemoveGlobalState`")]
    public static void RemoveGlobalValue(
        this IResolverContext context,
        string name)
        => RemoveGlobalState(context, name);

    public static void RemoveGlobalState(
        this IResolverContext context,
        string name)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw String_NullOrEmpty(nameof(name));
        }

        context.ContextData.Remove(name);
    }

    [Obsolete("Use `RemoveScopedState`")]
    public static void RemoveScopedValue(
        this IResolverContext context,
        string name)
        => RemoveScopedState(context, name);

    public static void RemoveScopedState(
        this IResolverContext context,
        string name)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw String_NullOrEmpty(nameof(name));
        }

        context.ScopedContextData = context.ScopedContextData.Remove(name);
    }

    [Obsolete("Use `RemoveLocalState`")]
    public static void RemoveLocalValue(
        this IResolverContext context,
        string name)
        => RemoveLocalState(context, name);

    public static void RemoveLocalState(
        this IResolverContext context,
        string name)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw String_NullOrEmpty(nameof(name));
        }

        context.LocalContextData = context.LocalContextData.Remove(name);
    }

    public static T GetEventMessage<T>(this IResolverContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.ScopedContextData.TryGetValue(
            WellKnownContextData.EventMessage,
            out var value) && value is { })
        {
            if (value is T casted)
            {
                return casted;
            }

            throw EventMessage_InvalidCast(typeof(T), value.GetType());
        }

        throw EventMessage_NotFound();
    }

    /// <summary>
    /// Gets the user for this request.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <returns>
    /// Returns the user for this request.
    /// </returns>
    public static ClaimsPrincipal? GetUser(this IResolverContext context)
        => context.GetGlobalValue<ClaimsPrincipal?>(nameof(ClaimsPrincipal));
}
