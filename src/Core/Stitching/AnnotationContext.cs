using System;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    internal class AnnotationContext
    {
        private AnnotationContext(ISchema schema)
            : this(schema, null)
        {
        }

        private AnnotationContext(ISchema schema, INamedType selectedType)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            SelectedType = selectedType;
        }

        public ISchema Schema { get; }

        public INamedType SelectedType { get; }

        public AnnotationContext WithType(INamedType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return new AnnotationContext(Schema, type);
        }

        public static AnnotationContext Create(ISchema schema)
        {
            return new AnnotationContext(schema);
        }
    }
}
