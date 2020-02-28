using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HotChocolate.Types
{
    public static class DirectiveCollectionExtensions
    {
        public static bool Any<T>(
            this IDirectiveCollection directives) =>
            Enumerable.Any(OfType<T>(directives));

        public static bool Any<T>(
            this IDirectiveCollection directives,
            Func<T, bool> predicate) =>
            Enumerable.Any(OfType<T>(directives), predicate);

        public static T First<T>(
            this IDirectiveCollection directives) =>
            Enumerable.First(OfType<T>(directives));

        public static T First<T>(
            this IDirectiveCollection directives,
            Func<T, bool> predicate) =>
            Enumerable.First(OfType<T>(directives), predicate);

        [return: MaybeNull]
        public static T FirstOrDefault<T>(
            this IDirectiveCollection directives) =>
            Enumerable.FirstOrDefault(OfType<T>(directives));

        [return: MaybeNull]
        public static T FirstOrDefault<T>(
            this IDirectiveCollection directives,
            Func<T, bool> predicate) =>
            Enumerable.FirstOrDefault(OfType<T>(directives), predicate);

        public static T Single<T>(
            this IDirectiveCollection directives) =>
            Enumerable.Single(OfType<T>(directives));

        public static T Single<T>(
            this IDirectiveCollection directives,
            Func<T, bool> predicate) =>
            Enumerable.Single(OfType<T>(directives), predicate);

        [return: MaybeNull]
        public static T SingleOrDefault<T>(
            this IDirectiveCollection directives) =>
            Enumerable.SingleOrDefault(OfType<T>(directives));

        [return: MaybeNull]
        public static T SingleOrDefault<T>(
            this IDirectiveCollection directives,
            Func<T, bool> predicate) =>
            Enumerable.SingleOrDefault(OfType<T>(directives), predicate);

        private static IEnumerable<T> OfType<T>(IDirectiveCollection directives)
        {
            foreach (IDirective directive in directives)
            {
                if (typeof(T).IsAssignableFrom(directive.Type.ClrType))
                {
                    yield return directive.ToObject<T>();
                }
            }
        }
    }
}
