using System;

namespace HotChocolate.Resolvers
{
    public class FieldReference
    {
        public FieldReference(string typeName, string fieldName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            TypeName = typeName;
            FieldName = fieldName;
        }

        public string TypeName { get; }
        public string FieldName { get; }
    }
}