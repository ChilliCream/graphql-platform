using System;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    public class SelectionContext
    {
        public SelectionContext(
            ObjectTypeDefinition TypeDefinition,
            FieldDefinition fieldDefinition,
            Field field)
        {
            TypeDefinition = TypeDefinition ?? throw new ArgumentNullException(nameof(TypeDefinition));
            FieldDefinition = fieldDefinition ?? throw new ArgumentNullException(nameof(fieldDefinition));
            Field = field ?? throw new ArgumentNullException(nameof(field));
        }

        public ObjectTypeDefinition TypeDefinition { get; }

        public FieldDefinition FieldDefinition { get; }

        public Field Field { get; }
    }
}