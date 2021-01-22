using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    /// <summary>
    /// Represents a entity that is used by a GraphQL client.
    /// </summary>
    public class EntityModel : ITypeModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EntityModel" />.
        /// </summary>
        /// <param name="type">
        /// The entity type.
        /// </param>
        public EntityModel(INamedType type)
        {
            Name = type.Name;
            Type = type;
            Definition = type.GetEntityDefinition();;
        }

        /// <summary>
        /// Gets the type name of the entity.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the entity type.
        /// </summary>
        public INamedType Type { get;}

        /// <summary>
        /// Gets the entity definition that specifies the fields that make up the id fields.
        /// </summary>
        public SelectionSetNode Definition { get; }
    }
}
