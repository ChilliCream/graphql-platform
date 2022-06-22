using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using HotChocolate.Resolvers;
using static HotChocolate.Utilities.ThrowHelper;
using static HotChocolate.Properties.TypeResources;

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

    /// <summary>
    /// Gets the global state for the specified <paramref name="name" />,
    /// or a default value if the state could not be resolved.
    /// </summary>
    /// <param name="context">The resovler context.</param>
    /// <param name="name">The name of the state.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the global state for the specified <paramref name="name" />
    /// or the default value of <typeparamref name="T" />, if the state
    /// could not be found or casted to <typeparamref name="T" />.
    /// </returns>
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

    /// <summary>
    /// Gets the global state for the specified <paramref name="name" />,
    /// or throws if the state does not exist.
    /// </summary>
    /// <param name="context">The resovler context.</param>
    /// <param name="name">The name of the state.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the global state for the specified <paramref name="name" />.
    /// </returns>
    [return: MaybeNull]
    public static T GetGlobalState<T>(
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

        if (context.ContextData.TryGetValue(name, out var value))
        {
            if (value is null)
            {
                return default;
            }

            if (value is T typedValue)
            {
                return typedValue;
            }
        }

        throw new ArgumentException(
            string.Format(ResolverContextExtensions_ContextData_KeyNotFound, name));
    }

    [Obsolete("Use `GetScopedStateOrDefault`")]
    [return: MaybeNull]
    public static T GetScopedValue<T>(
        this IResolverContext context,
        string name)
        => GetScopedStateOrDefault<T>(context, name);

    /// <summary>
    /// Gets the scoped state for the specified <paramref name="name" />,
    /// or a default value if the state could not be resolved.
    /// </summary>
    /// <param name="context">The resovler context.</param>
    /// <param name="name">The name of the state.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the scoped state for the specified <paramref name="name" />
    /// or the default value of <typeparamref name="T" />, if the state
    /// could not be found or casted to <typeparamref name="T" />.
    /// </returns>
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

    /// <summary>
    /// Gets the scoped state for the specified <paramref name="name" />,
    /// or throws if the state does not exist.
    /// </summary>
    /// <param name="context">The resovler context.</param>
    /// <param name="name">The name of the state.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the scoped state for the specified <paramref name="name" />.
    /// </returns>
    [return: MaybeNull]
    public static T GetScopedState<T>(
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

        if (context.ScopedContextData.TryGetValue(name, out var value))
        {
            if (value is null)
            {
                return default;
            }

            if (value is T typedValue)
            {
                return typedValue;
            }
        }

        throw new ArgumentException(
            string.Format(ResolverContextExtensions_ScopedContextData_KeyNotFound, name));
    }

    [Obsolete("Use `GetLocalStateOrDefault`")]
    [return: MaybeNull]
    public static T GetLocalValue<T>(
        this IResolverContext context,
        string name)
        => GetLocalStateOrDefault<T>(context, name);

    /// <summary>
    /// Gets the local state for the specified <paramref name="name" />,
    /// or a default value if the state could not be resolved.
    /// </summary>
    /// <param name="context">The resovler context.</param>
    /// <param name="name">The name of the state.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the local state for the specified <paramref name="name" />
    /// or the default value of <typeparamref name="T" />, if the state
    /// could not be found or casted to <typeparamref name="T" />.
    /// </returns>
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

    /// <summary>
    /// Gets the local state for the specified <paramref name="name" />,
    /// or throws if the state does not exist.
    /// </summary>
    /// <param name="context">The resovler context.</param>
    /// <param name="name">The name of the state.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the local state for the specified <paramref name="name" />.
    /// </returns>
    [return: MaybeNull]
    public static T GetLocalState<T>(
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

        if (context.LocalContextData.TryGetValue(name, out var value))
        {
            if (value is null)
            {
                return default;
            }

            if (value is T typedValue)
            {
                return typedValue;
            }
        }

        throw new ArgumentException(
            string.Format(ResolverContextExtensions_LocalContextData_KeyNotFound, name));
    }

    [Obsolete("Use `SetGlobalState`")]
    public static void SetGlobalValue<T>(
        this IResolverContext context,
        string name,
        [MaybeNull] T value)
        => SetGlobalState(context, name, value);

    /// <summary>
    /// Sets the global state for <paramref name="name" />
    /// to the specified <paramref name="value" />.
    /// State set previously using the same <paramref name="name" />
    /// will be overwritten.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <param name="value">The new state value.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
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

    /// <summary>
    /// Sets the scoped state for <paramref name="name" />
    /// to the specified <paramref name="value" />.
    /// State set previously using the same <paramref name="name" />
    /// will be overwritten.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <param name="value">The new state value.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
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

    /// <summary>
    /// Sets the local state for <paramref name="name" />
    /// to the specified <paramref name="value" />.
    /// State set previously using the same <paramref name="name" />
    /// will be overwritten.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <param name="value">The new state value.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
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

    [Obsolete("Use `GetOrSetGlobalState`")]
    [return: MaybeNull]
    public static T GetOrAddGlobalValue<T>(
        this IResolverContext context,
        string name,
        Func<string, T> createValue)
        => GetOrSetGlobalState(context, name, createValue);

    /// <summary>
    /// Gets or sets the global state for the specified <paramref name="name" />.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <param name="createValue">
    /// A function that receives the name of the state as an argument
    /// and returns the new state value that should be set.
    /// </param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// The existing state for the specified <paramref name="name" />,
    /// or the newly created state using the <paramref name="createValue" /> function.
    /// </returns>
    [return: MaybeNull]
    public static T GetOrSetGlobalState<T>(
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
            var newValue = createValue(name);
            SetGlobalState(context, name, newValue);
            return newValue;
        }
    }

    [Obsolete("Use `GetOrSetScopedState`")]
    [return: MaybeNull]
    public static T GetOrAddScopedValue<T>(
        this IResolverContext context,
        string name,
        Func<string, T> createValue)
        => GetOrSetScopedState(context, name, createValue);

    /// <summary>
    /// Gets or sets the scoped state for the specified <paramref name="name" />.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <param name="createValue">
    /// A function that receives the name of the state as an argument
    /// and returns the new state value that should be set.
    /// </param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// The existing state for the specified <paramref name="name" />,
    /// or the newly created state using the <paramref name="createValue" /> function.
    /// </returns>
    [return: MaybeNull]
    public static T GetOrSetScopedState<T>(
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
            var newValue = createValue(name);
            SetScopedState(context, name, newValue);
            return newValue;
        }
    }

    [Obsolete("Use `GetOrSetLocalState`")]
    [return: MaybeNull]
    public static T GetOrAddLocalValue<T>(
        this IResolverContext context,
        string name,
        Func<string, T> createValue)
        => GetOrSetLocalState(context, name, createValue);

    /// <summary>
    /// Gets or sets the local state for the specified <paramref name="name" />.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <param name="createValue">
    /// A function that receives the name of the state as an argument
    /// and returns the new state value that should be set.
    /// </param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// The existing state for the specified <paramref name="name" />,
    /// or the newly created state using the <paramref name="createValue" /> function.
    /// </returns>
    [return: MaybeNull]
    public static T GetOrSetLocalState<T>(
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
            var newValue = createValue(name);
            SetLocalState(context, name, newValue);
            return newValue;
        }
    }

    [Obsolete]
    public static void RemoveGlobalValue(
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

    /// <summary>
    /// Removes the scoped state set for the specified <paramref name="name" />.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
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

    /// <summary>
    /// Removes the local state set for the specified <paramref name="name" />.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
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

    /// <summary>
    /// Gets the event message.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <typeparam name="T">The type of the event message.</typeparam>
    /// <returns>The event message.</returns>
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
        => context.GetGlobalStateOrDefault<ClaimsPrincipal?>(nameof(ClaimsPrincipal));
}
