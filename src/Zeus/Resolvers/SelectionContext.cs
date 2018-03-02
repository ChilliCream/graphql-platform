using System;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    public class SelectionContext
    {
        public SelectionContext(
            ObjectTypeDefinition typeDefinition,
            FieldDefinition fieldDefinition,
            Field field)
        {
            TypeDefinition = typeDefinition ?? throw new ArgumentNullException(nameof(typeDefinition));
            FieldDefinition = fieldDefinition ?? throw new ArgumentNullException(nameof(fieldDefinition));
            Field = field ?? throw new ArgumentNullException(nameof(field));
        }

        public ObjectTypeDefinition TypeDefinition { get; }

        public FieldDefinition FieldDefinition { get; }

        public Field Field { get; }
    }
}