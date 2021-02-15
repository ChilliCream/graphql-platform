using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Represents the entity for which the ID shall be generated or an id field of that entity.
    /// </summary>
    public class EntityIdDescriptor
    {
        /// <summary>
        /// Creates a new instance of <see cref="EntityIdDescriptor"/>.
        /// </summary>
        /// <param name="name">
        /// The name of the field or entity.
        /// </param>
        /// <param name="typeName">
        /// The serialization type name of the entity id field, eg. String.
        /// </param>
        /// <param name="fields">
        /// The child fields.
        /// </param>
        public EntityIdDescriptor(
            string name, 
            string typeName, 
            IReadOnlyList<EntityIdDescriptor>? fields = null)
        {
            Name = name;
            TypeName = typeName;
            Fields = fields ?? Array.Empty<EntityIdDescriptor>();
        }

        /// <summary>
        /// Gets the name of the field or entity.
        /// </summary>
        /// <value></value>
        public string Name { get; }

        /// <summary>
        /// Gets the serialization type name of the entity id field, eg. String.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Gets the child fields.
        /// </summary>
        public IReadOnlyList<EntityIdDescriptor> Fields { get; }
    }
}
