using System;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    internal class AnnotationContext
    {
        public AnnotationContext(ISchema schema)
            : this(schema, null)
        {
        }

        public AnnotationContext(ISchema schema, INamedType selectedType)
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
    }
}
