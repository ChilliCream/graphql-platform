using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Language
{
    internal static class DocumentWriterExtensions
    {
        public static void WriteMany<T>(
            this DocumentWriter writer,
            IEnumerable<T> items,
            Action<T> action)
        {
            WriteMany(writer, items, action, ", ");
        }

        public static void WriteMany<T>(
            this DocumentWriter writer,
            IEnumerable<T> items,
            Action<T> action,
            string separator)
        {
            if (items.Any())
            {
                action(items.First());

                foreach (T item in items.Skip(1))
                {
                    writer.Write(separator);
                    action(item);
                }
            }
        }

        public static void WriteMany<T>(
            this DocumentWriter writer,
            IEnumerable<T> items,
            Action<T> action,
            Action separator)
        {
            if (items.Any())
            {
                action(items.First());

                foreach (T item in items.Skip(1))
                {
                    separator();
                    action(item);
                }
            }
        }
    }
}
