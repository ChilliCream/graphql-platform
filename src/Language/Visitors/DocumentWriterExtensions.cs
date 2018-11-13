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
            Action<T, DocumentWriter> action)
        {
            WriteMany(writer, items, action, ", ");
        }

        public static void WriteMany<T>(
            this DocumentWriter writer,
            IEnumerable<T> items,
            Action<T, DocumentWriter> action,
            string separator)
        {
            if (items.Any())
            {
                action(items.First(), writer);

                foreach (T item in items.Skip(1))
                {
                    writer.Write(separator);
                    action(item, writer);
                }
            }
        }

        public static void WriteMany<T>(
            this DocumentWriter writer,
            IEnumerable<T> items,
            Action<T, DocumentWriter> action,
            Action<DocumentWriter> separator)
        {
            if (items.Any())
            {
                action(items.First(), writer);

                foreach (T item in items.Skip(1))
                {
                    separator(writer);
                    action(item, writer);
                }
            }
        }
    }
}
