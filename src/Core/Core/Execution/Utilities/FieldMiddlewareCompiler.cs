using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class FieldMiddlewareCompiler
    {
        private readonly Dictionary<ObjectField, FieldDelegate> _cache =
            new Dictionary<ObjectField, FieldDelegate>();

        public FieldMiddlewareCompiler(
            ISchema schema,
            FieldMiddleware fieldMiddleware)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (fieldMiddleware == null)
            {
                throw new ArgumentNullException(nameof(fieldMiddleware));
            }

            PopulateCache(schema, fieldMiddleware, _cache);
        }

        public FieldDelegate GetMiddleware(ObjectField field)
        {
            return _cache[field];
        }

        private static void PopulateCache(
            ISchema schema,
            FieldMiddleware fieldMiddleware,
            Dictionary<ObjectField, FieldDelegate> cache)
        {
            foreach (ObjectType type in schema.Types
                .OfType<ObjectType>())
            {
                foreach (ObjectField field in type.Fields)
                {
                    FieldDelegate fieldDelegate =
                        field.IsIntrospectionField || type.IsIntrospectionType()
                        ? field.Middleware
                        : fieldMiddleware.Invoke(field.Middleware);
                    cache.Add(field, fieldDelegate);
                }
            }
        }
    }
}
