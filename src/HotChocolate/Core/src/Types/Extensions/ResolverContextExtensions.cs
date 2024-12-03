using System.Security.Claims;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Utilities.ThrowHelper;
using static HotChocolate.Properties.TypeResources;

#nullable enable

namespace HotChocolate;

public static class ResolverContextExtensions
{
    /// <summary>
    /// Gets the global state for the specified <paramref name="name" />,
    /// or a default value if the state could not be resolved.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the global state for the specified <paramref name="name" />
    /// or the default value of <typeparamref name="T" />, if the state
    /// could not be found or casted to <typeparamref name="T" />.
    /// </returns>
    public static T? GetGlobalStateOrDefault<T>(
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
    /// or a default value if the state could not be resolved.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the global state for the specified <paramref name="name" />
    /// or the default value of <typeparamref name="T" />, if the state
    /// could not be found or casted to <typeparamref name="T" />.
    /// </returns>
    public static T GetGlobalStateOrDefault<T>(
        this IResolverContext context,
        string name,
        T defaultValue)
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

        return defaultValue;
    }

    /// <summary>
    /// Gets the global state for the specified <paramref name="name" />,
    /// or throws if the state does not exist.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the global state for the specified <paramref name="name" />.
    /// </returns>
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

        if (context.ContextData.TryGetValue(name, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        throw new ArgumentException(
            string.Format(ResolverContextExtensions_ContextData_KeyNotFound, name));
    }

    /// <summary>
    /// Gets the scoped state for the specified <paramref name="name" />,
    /// or a default value if the state could not be resolved.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the scoped state for the specified <paramref name="name" />
    /// or the default value of <typeparamref name="T" />, if the state
    /// could not be found or casted to <typeparamref name="T" />.
    /// </returns>
    public static T? GetScopedStateOrDefault<T>(
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
    /// or a default value if the state could not be resolved.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the scoped state for the specified <paramref name="name" />
    /// or the default value of <typeparamref name="T" />, if the state
    /// could not be found or casted to <typeparamref name="T" />.
    /// </returns>
    public static T GetScopedStateOrDefault<T>(
        this IResolverContext context,
        string name,
        T defaultValue)
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

        return defaultValue;
    }

    /// <summary>
    /// Gets the scoped state for the specified <paramref name="name" />,
    /// or throws if the state does not exist.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the scoped state for the specified <paramref name="name" />.
    /// </returns>
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

        if (context.ScopedContextData.TryGetValue(name, out var value) &&
            value is T typedValue)
        {
            return typedValue;
        }

        throw new ArgumentException(
            string.Format(ResolverContextExtensions_ScopedContextData_KeyNotFound, name));
    }

    /// <summary>
    /// Gets the local state for the specified <paramref name="name" />,
    /// or a default value if the state could not be resolved.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the local state for the specified <paramref name="name" />
    /// or the default value of <typeparamref name="T" />, if the state
    /// could not be found or casted to <typeparamref name="T" />.
    /// </returns>
    public static T? GetLocalStateOrDefault<T>(
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
    /// or a default value if the state could not be resolved.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the local state for the specified <paramref name="name" />
    /// or the default value of <typeparamref name="T" />, if the state
    /// could not be found or casted to <typeparamref name="T" />.
    /// </returns>
    public static T GetLocalStateOrDefault<T>(
        this IResolverContext context,
        string name,
        T defaultValue)
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

        return defaultValue;
    }

    /// <summary>
    /// Gets the local state for the specified <paramref name="name" />,
    /// or throws if the state does not exist.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <param name="name">The name of the state.</param>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <returns>
    /// Returns the local state for the specified <paramref name="name" />.
    /// </returns>
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

        if (context.LocalContextData.TryGetValue(name, out var value) &&
            value is T casted)
        {
            return casted;
        }

        throw new ArgumentException(
            string.Format(ResolverContextExtensions_LocalContextData_KeyNotFound, name));
    }

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
        T value)
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
        T value)
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
        T value)
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

        var newValue = createValue(name);
        SetGlobalState(context, name, newValue);
        return newValue;
    }

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

        var newValue = createValue(name);
        SetScopedState(context, name, newValue);
        return newValue;
    }

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

        var newValue = createValue(name);
        SetLocalState(context, name, newValue);
        return newValue;
    }

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

    /// <summary>
    /// Checks if a field is selected in the current selection set.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="fieldName">
    /// The name of the field that shall be checked.
    /// </param>
    /// <returns>
    /// <c>true</c> if the field is selected; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context" /> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldName" /> is <c>null</c> or whitespace.
    /// </exception>
    public static bool IsSelected(this IResolverContext context, string fieldName)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException(
                ResolverContextExtensions_IsSelected_FieldNameEmpty,
                nameof(fieldName));
        }

        if (!context.Selection.Type.IsCompositeType())
        {
            return false;
        }

        var namedType = context.Selection.Type.NamedType();

        if (namedType.IsAbstractType())
        {
            foreach (var possibleType in context.Schema.GetPossibleTypes(namedType))
            {
                var selections = context.GetSelections(possibleType, context.Selection);

                for (var i = 0; i < selections.Count; i++)
                {
                    if (selections[i].Field.Name.Equals(fieldName))
                    {
                        return true;
                    }
                }
            }
        }
        else
        {
            var selections = context.GetSelections((ObjectType)namedType, context.Selection);

            for (var i = 0; i < selections.Count; i++)
            {
                if (selections[i].Field.Name.Equals(fieldName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a field is selected in the current selection set.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="fieldName1">
    /// The name of the first field that shall be checked.
    /// </param>
    /// <param name="fieldName2">
    /// The name of the second field that shall be checked.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context" /> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldName1" /> is <c>null</c> or whitespace or
    /// <paramref name="fieldName2" /> is <c>null</c> or whitespace.
    /// </exception>
    public static bool IsSelected(this IResolverContext context, string fieldName1, string fieldName2)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrWhiteSpace(fieldName1))
        {
            throw new ArgumentException(
                ResolverContextExtensions_IsSelected_FieldNameEmpty,
                nameof(fieldName1));
        }

        if (string.IsNullOrWhiteSpace(fieldName2))
        {
            throw new ArgumentException(
                ResolverContextExtensions_IsSelected_FieldNameEmpty,
                nameof(fieldName2));
        }

        if (!context.Selection.Type.IsCompositeType())
        {
            return false;
        }

        var namedType = context.Selection.Type.NamedType();

        if (namedType.IsAbstractType())
        {
            foreach (var possibleType in context.Schema.GetPossibleTypes(namedType))
            {
                var selections = context.GetSelections(possibleType, context.Selection);

                for (var i = 0; i < selections.Count; i++)
                {
                    var selection = selections[i];

                    if (selection.Field.Name.Equals(fieldName1) || selection.Field.Name.Equals(fieldName2))
                    {
                        return true;
                    }
                }
            }
        }
        else
        {
            var selections = context.GetSelections((ObjectType)namedType, context.Selection);

            for (var i = 0; i < selections.Count; i++)
            {
                var selection = selections[i];

                if (selection.Field.Name.Equals(fieldName1) || selection.Field.Name.Equals(fieldName2))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a field is selected in the current selection set.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="fieldName1">
    /// The name of the first field that shall be checked.
    /// </param>
    /// <param name="fieldName2">
    /// The name of the second field that shall be checked.
    /// </param>
    /// <param name="fieldName3">
    /// The name of the third field that shall be checked.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context" /> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldName1" /> is <c>null</c> or whitespace or
    /// <paramref name="fieldName2" /> is <c>null</c> or whitespace or
    /// <paramref name="fieldName3" /> is <c>null</c> or whitespace.
    /// </exception>
    public static bool IsSelected(
        this IResolverContext context,
        string fieldName1,
        string fieldName2,
        string fieldName3)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrWhiteSpace(fieldName1))
        {
            throw new ArgumentException(
                ResolverContextExtensions_IsSelected_FieldNameEmpty,
                nameof(fieldName1));
        }

        if (string.IsNullOrWhiteSpace(fieldName2))
        {
            throw new ArgumentException(
                ResolverContextExtensions_IsSelected_FieldNameEmpty,
                nameof(fieldName2));
        }

        if(string.IsNullOrWhiteSpace(fieldName3))
        {
            throw new ArgumentException(
                ResolverContextExtensions_IsSelected_FieldNameEmpty,
                nameof(fieldName3));
        }

        if (!context.Selection.Type.IsCompositeType())
        {
            return false;
        }

        var namedType = context.Selection.Type.NamedType();

        if (namedType.IsAbstractType())
        {
            foreach (var possibleType in context.Schema.GetPossibleTypes(namedType))
            {
                var selections = context.GetSelections(possibleType, context.Selection);

                for (var i = 0; i < selections.Count; i++)
                {
                    var selection = selections[i];

                    if (selection.Field.Name.Equals(fieldName1) ||
                        selection.Field.Name.Equals(fieldName2) ||
                        selection.Field.Name.Equals(fieldName3))
                    {
                        return true;
                    }
                }
            }
        }
        else
        {
            var selections = context.GetSelections((ObjectType)namedType, context.Selection);

            for (var i = 0; i < selections.Count; i++)
            {
                var selection = selections[i];

                if (selection.Field.Name.Equals(fieldName1) ||
                    selection.Field.Name.Equals(fieldName2) ||
                    selection.Field.Name.Equals(fieldName3))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a field is selected in the current selection set.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="fieldNames">
    /// The names of the fields that shall be checked.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context" /> is <c>null</c> or
    /// <paramref name="fieldNames" /> is <c>null</c>.
    /// </exception>
    public static bool IsSelected(
        this IResolverContext context,
        ISet<string> fieldNames)
    {
        if(context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if(fieldNames is null)
        {
            throw new ArgumentNullException(nameof(fieldNames));
        }

        if (!context.Selection.Type.IsCompositeType())
        {
            return false;
        }

        var namedType = context.Selection.Type.NamedType();

        if (namedType.IsAbstractType())
        {
            foreach (var possibleType in context.Schema.GetPossibleTypes(namedType))
            {
                var selections = context.GetSelections(possibleType, context.Selection);

                for (var i = 0; i < selections.Count; i++)
                {
                    if (fieldNames.Contains(selections[i].Field.Name))
                    {
                        return true;
                    }
                }
            }
        }
        else
        {
            var selections = context.GetSelections((ObjectType)namedType, context.Selection);

            for (var i = 0; i < selections.Count; i++)
            {
                if (fieldNames.Contains(selections[i].Field.Name))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
