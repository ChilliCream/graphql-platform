using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Descriptors
{
    /// <summary>
    /// Represents the code descriptor of the entity id factor.
    /// </summary>
    public class EntityIdFactoryDescriptor : ICodeDescriptor
    {
        /// <summary>
        /// Creates a new instance of <see cref="EntityIdFactoryDescriptor"/>.
        /// </summary>
        /// <param name="name">
        /// The class name of the entity id factory.
        /// </param>
        /// <param name="entities">
        /// The entity descriptors.
        /// </param>
        /// <param name="namespace">
        /// The namespace of this class.
        /// </param>
        public EntityIdFactoryDescriptor(
            NameString name,
            IReadOnlyList<EntityIdDescriptor> entities,
            string @namespace)
        {
            Name = name;
            Entities = entities;
            Namespace = @namespace;
        }

        /// <summary>
        /// Gets the class name of the entity id factory.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the namespace of this factory class.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Gets the entity descriptors.
        /// </summary>
        public IReadOnlyList<EntityIdDescriptor> Entities { get; }
    }
}
