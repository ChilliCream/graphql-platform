using System.Security.Claims;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Utilities.ThrowHelper;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate;

public static class ResolverContextExtensions
{
    extension(IResolverContext context)
    {
        /// <summary>
        /// Gets the global state for the specified <paramref name="name" />,
        /// or a default value if the state could not be resolved.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        /// <typeparam name="T">The type of the state.</typeparam>
        /// <returns>
        /// Returns the global state for the specified <paramref name="name" />
        /// or the default value of <typeparamref name="T" />, if the state
        /// could not be found or cast to <typeparamref name="T" />.
        /// </returns>
        public T? GetGlobalStateOrDefault<T>(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (context.ContextData.TryGetValue(name, out var value)
                && value is T casted)
            {
                return casted;
            }

            return default;
        }

        /// <summary>
        /// Gets the global state for the specified <paramref name="name" />,
        /// or a default value if the state could not be resolved.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <typeparam name="T">The type of the state.</typeparam>
        /// <returns>
        /// Returns the global state for the specified <paramref name="name" />
        /// or the default value of <typeparamref name="T" />, if the state
        /// could not be found or cast to <typeparamref name="T" />.
        /// </returns>
        public T GetGlobalStateOrDefault<T>(string name, T defaultValue)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (context.ContextData.TryGetValue(name, out var value)
                && value is T casted)
            {
                return casted;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets the global state for the specified <paramref name="name" />,
        /// or throws if the state does not exist.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        /// <typeparam name="T">The type of the state.</typeparam>
        /// <returns>
        /// Returns the global state for the specified <paramref name="name" />.
        /// </returns>
        public T GetGlobalState<T>(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

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
        /// <param name="name">The name of the state.</param>
        /// <typeparam name="T">The type of the state.</typeparam>
        /// <returns>
        /// Returns the scoped state for the specified <paramref name="name" />
        /// or the default value of <typeparamref name="T" />, if the state
        /// could not be found or cast to <typeparamref name="T" />.
        /// </returns>
        public T? GetScopedStateOrDefault<T>(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (context.ScopedContextData.TryGetValue(name, out var value)
                && value is T casted)
            {
                return casted;
            }

            return default;
        }

        /// <summary>
        /// Gets the scoped state for the specified <paramref name="name" />,
        /// or a default value if the state could not be resolved.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <typeparam name="T">The type of the state.</typeparam>
        /// <returns>
        /// Returns the scoped state for the specified <paramref name="name" />
        /// or the default value of <typeparamref name="T" />, if the state
        /// could not be found or cast to <typeparamref name="T" />.
        /// </returns>
        public T GetScopedStateOrDefault<T>(string name, T defaultValue)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (context.ScopedContextData.TryGetValue(name, out var value)
                && value is T casted)
            {
                return casted;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets the scoped state for the specified <paramref name="name" />,
        /// or throws if the state does not exist.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        /// <typeparam name="T">The type of the state.</typeparam>
        /// <returns>
        /// Returns the scoped state for the specified <paramref name="name" />.
        /// </returns>
        public T GetScopedState<T>(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (context.ScopedContextData.TryGetValue(name, out var value)
                && value is T typedValue)
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
        /// <param name="name">The name of the state.</param>
        /// <typeparam name="T">The type of the state.</typeparam>
        /// <returns>
        /// Returns the local state for the specified <paramref name="name" />
        /// or the default value of <typeparamref name="T" />, if the state
        /// could not be found or cast to <typeparamref name="T" />.
        /// </returns>
        public T? GetLocalStateOrDefault<T>(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (context.LocalContextData.TryGetValue(name, out var value)
                && value is T casted)
            {
                return casted;
            }

            return default;
        }

        /// <summary>
        /// Gets the local state for the specified <paramref name="name" />,
        /// or a default value if the state could not be resolved.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <typeparam name="T">The type of the state.</typeparam>
        /// <returns>
        /// Returns the local state for the specified <paramref name="name" />
        /// or the default value of <typeparamref name="T" />, if the state
        /// could not be found or cast to <typeparamref name="T" />.
        /// </returns>
        public T GetLocalStateOrDefault<T>(string name, T defaultValue)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (context.LocalContextData.TryGetValue(name, out var value)
                && value is T casted)
            {
                return casted;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets the local state for the specified <paramref name="name" />,
        /// or throws if the state does not exist.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        /// <typeparam name="T">The type of the state.</typeparam>
        /// <returns>
        /// Returns the local state for the specified <paramref name="name" />.
        /// </returns>
        public T GetLocalState<T>(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (context.LocalContextData.TryGetValue(name, out var value)
                && value is T casted)
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
        /// <param name="name">The name of the state.</param>
        /// <param name="value">The new state value.</param>
        /// <typeparam name="T">The type of the state.</typeparam>
        public void SetGlobalState<T>(string name, T value)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            context.ContextData[name] = value;
        }

        /// <summary>
        /// Sets the scoped state for <paramref name="name" />
        /// to the specified <paramref name="value" />.
        /// State set previously using the same <paramref name="name" />
        /// will be overwritten.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        /// <param name="value">The new state value.</param>
        /// <typeparam name="T">The type of the state.</typeparam>
        public void SetScopedState<T>(string name, T value)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            context.ScopedContextData = context.ScopedContextData.SetItem(name, value);
        }

        /// <summary>
        /// Sets the local state for <paramref name="name" />
        /// to the specified <paramref name="value" />.
        /// State set previously using the same <paramref name="name" />
        /// will be overwritten.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        /// <param name="value">The new state value.</param>
        /// <typeparam name="T">The type of the state.</typeparam>
        public void SetLocalState<T>(string name, T value)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            context.LocalContextData = context.LocalContextData.SetItem(name, value);
        }

        /// <summary>
        /// Gets or sets the global state for the specified <paramref name="name" />.
        /// </summary>
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
        public T GetOrSetGlobalState<T>(string name, Func<string, T> createValue)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(createValue);

            if (context.ContextData.TryGetValue(name, out var value)
                && value is T casted)
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
        public T GetOrSetScopedState<T>(string name, Func<string, T> createValue)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(createValue);

            if (context.ScopedContextData.TryGetValue(name, out var value)
                && value is T casted)
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
        public T GetOrSetLocalState<T>(string name, Func<string, T> createValue)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(createValue);

            if (context.LocalContextData.TryGetValue(name, out var value)
                && value is T casted)
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
        /// <param name="name">The name of the state.</param>
        public void RemoveScopedState(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            context.ScopedContextData = context.ScopedContextData.Remove(name);
        }

        /// <summary>
        /// Removes the local state set for the specified <paramref name="name" />.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        public void RemoveLocalState(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            context.LocalContextData = context.LocalContextData.Remove(name);
        }

        /// <summary>
        /// Gets the event message.
        /// </summary>
        /// <typeparam name="T">The type of the event message.</typeparam>
        /// <returns>The event message.</returns>
        public T GetEventMessage<T>()
        {
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
        /// <returns>
        /// Returns the user for this request.
        /// </returns>
        public ClaimsPrincipal? GetUser()
            => context.GetGlobalStateOrDefault<ClaimsPrincipal?>(nameof(ClaimsPrincipal));

        /// <summary>
        /// Checks if a field is selected in the current selection set.
        /// </summary>
        /// <param name="fieldName">
        /// The name of the field that shall be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if the field is selected; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="fieldName" /> is <c>null</c> or whitespace.
        /// </exception>
        public bool IsSelected(string fieldName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            var namedType = context.Selection.Type.NamedType();

            if (!namedType.IsCompositeType())
            {
                return false;
            }

            if (namedType.IsAbstractType())
            {
                foreach (var possibleType in context.Schema.GetPossibleTypes(namedType))
                {
                    var includeFlags = context.IncludeFlags;
                    var selectionSet = context.Operation.GetSelectionSet(context.Selection, possibleType);

                    foreach (var selection in selectionSet.Selections)
                    {
                        if (selection.IsIncluded(includeFlags)
                            && selection.Field.Name.Equals(fieldName))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                var includeFlags = context.IncludeFlags;
                var selectionSet = context.Operation.GetSelectionSet(context.Selection, (ObjectType)namedType);

                foreach (var selection in selectionSet.Selections)
                {
                    if (selection.IsIncluded(includeFlags)
                        && selection.Field.Name.Equals(fieldName))
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
        /// <param name="fieldName1">
        /// The name of the first field that shall be checked.
        /// </param>
        /// <param name="fieldName2">
        /// The name of the second field that shall be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if one of the fields is selected; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="fieldName1" /> is <c>null</c> or whitespace or
        /// <paramref name="fieldName2" /> is <c>null</c> or whitespace.
        /// </exception>
        public bool IsSelected(string fieldName1, string fieldName2)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName1);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName2);

            var namedType = context.Selection.Type.NamedType();

            if (!namedType.IsCompositeType())
            {
                return false;
            }

            if (namedType.IsAbstractType())
            {
                foreach (var possibleType in context.Schema.GetPossibleTypes(namedType))
                {
                    var includeFlags = context.IncludeFlags;
                    var selectionSet = context.Operation.GetSelectionSet(context.Selection, possibleType);

                    foreach (var selection in selectionSet.Selections)
                    {
                        if (selection.IsSkipped(includeFlags))
                        {
                            continue;
                        }

                        if (selection.Field.Name.Equals(fieldName1) || selection.Field.Name.Equals(fieldName2))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                var includeFlags = context.IncludeFlags;
                var selectionSet = context.Operation.GetSelectionSet(context.Selection, (ObjectType)namedType);

                foreach (var selection in selectionSet.Selections)
                {
                    if (selection.IsSkipped(includeFlags))
                    {
                        continue;
                    }

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
        /// <param name="fieldName1">
        /// The name of the first field that shall be checked.
        /// </param>
        /// <param name="fieldName2">
        /// The name of the second field that shall be checked.
        /// </param>
        /// <param name="fieldName3">
        /// The name of the third field that shall be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if one of the fields is selected; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="fieldName1" /> is <c>null</c> or whitespace or
        /// <paramref name="fieldName2" /> is <c>null</c> or whitespace or
        /// <paramref name="fieldName3" /> is <c>null</c> or whitespace.
        /// </exception>
        public bool IsSelected(string fieldName1, string fieldName2, string fieldName3)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName1);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName2);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName3);

            var namedType = context.Selection.Type.NamedType();

            if (!namedType.IsCompositeType())
            {
                return false;
            }

            if (namedType.IsAbstractType())
            {
                foreach (var possibleType in context.Schema.GetPossibleTypes(namedType))
                {
                    var includeFlags = context.IncludeFlags;
                    var selectionSet = context.Operation.GetSelectionSet(context.Selection, possibleType);

                    foreach (var selection in selectionSet.Selections)
                    {
                        if (selection.IsSkipped(includeFlags))
                        {
                            continue;
                        }

                        if (selection.Field.Name.Equals(fieldName1)
                            || selection.Field.Name.Equals(fieldName2)
                            || selection.Field.Name.Equals(fieldName3))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                var includeFlags = context.IncludeFlags;
                var selectionSet = context.Operation.GetSelectionSet(context.Selection, (ObjectType)namedType);

                foreach (var selection in selectionSet.Selections)
                {
                    if (selection.IsSkipped(includeFlags))
                    {
                        continue;
                    }

                    if (selection.Field.Name.Equals(fieldName1)
                        || selection.Field.Name.Equals(fieldName2)
                        || selection.Field.Name.Equals(fieldName3))
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
        /// <param name="fieldNames">
        /// The names of the fields that shall be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if one of the fields is selected; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fieldNames" /> is <c>null</c>.
        /// </exception>
        public bool IsSelected(ISet<string> fieldNames)
        {
            ArgumentNullException.ThrowIfNull(fieldNames);

            var namedType = context.Selection.Type.NamedType();

            if (!namedType.IsCompositeType())
            {
                return false;
            }

            if (namedType.IsAbstractType())
            {
                foreach (var possibleType in context.Schema.GetPossibleTypes(namedType))
                {
                    var includeFlags = context.IncludeFlags;
                    var selectionSet = context.Operation.GetSelectionSet(context.Selection, possibleType);

                    foreach (var selection in selectionSet.Selections)
                    {
                        if (selection.IsIncluded(includeFlags)
                            && fieldNames.Contains(selection.Field.Name))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                var includeFlags = context.IncludeFlags;
                var selectionSet = context.Operation.GetSelectionSet(context.Selection, (ObjectType)namedType);

                foreach (var selection in selectionSet.Selections)
                {
                    if (selection.IsIncluded(includeFlags)
                        && fieldNames.Contains(selection.Field.Name))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
