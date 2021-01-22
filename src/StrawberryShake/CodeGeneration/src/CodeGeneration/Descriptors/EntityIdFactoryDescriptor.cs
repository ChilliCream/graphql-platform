using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
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
        public EntityIdFactoryDescriptor(
            NameString name,
            IReadOnlyList<EntityIdDescriptor> entities)
        {
            Name = name;
            Entities = entities;
        }

        /// <summary>
        /// Gets the class name of the entity id factory.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the entity descriptors.
        /// </summary>
        public IReadOnlyList<EntityIdDescriptor> Entities { get; }
    }
}
