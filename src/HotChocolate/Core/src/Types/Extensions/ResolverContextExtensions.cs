using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate
{
    public static class ResolverContextExtensions
    {
        [return: MaybeNull]
        public static T GetGlobalValue<T>(
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

        [return: MaybeNull]
        public static T GetScopedValue<T>(
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

        [return: MaybeNull]
        public static T GetLocalValue<T>(
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

        public static void SetGlobalValue<T>(
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

        public static void SetScopedValue<T>(
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

        public static void SetLocalValue<T>(
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

        [return: MaybeNull]
        public static T GetOrAddGlobalValue<T>(
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
                context.ContextData[name] = newValue;
                return newValue;
            }
        }

        [return: MaybeNull]
        public static T GetOrAddScopedValue<T>(
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
                SetScopedValue(context, name, newValue);
                return newValue;
            }
        }

        [return: MaybeNull]
        public static T GetOrAddLocalValue<T>(
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
                SetLocalValue(context, name, newValue);
                return newValue;
            }
        }

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

        public static void RemoveScopedValue(
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

        public static void RemoveLocalValue(
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
                if(value is T casted)
                {
                    return casted;
                }

                throw EventMessage_InvalidCast(typeof(T), value.GetType());
            }

            throw EventMessage_NotFound();
        }
    }
}
