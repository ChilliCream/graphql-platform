using System;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    internal class SchemaContextInfo
    {
        public SchemaContextInfo(ISchema schema, ObjectType objectType, Field field)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            Schema = schema;
            ObjectType = objectType;
            Field = field;
        }

        public ISchema Schema { get; }

        public ObjectType ObjectType { get; }

        public Field Field { get; }
    }

}
