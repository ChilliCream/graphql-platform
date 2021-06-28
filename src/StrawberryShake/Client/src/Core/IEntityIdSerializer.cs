using System.Text.Json;

namespace StrawberryShake
{
    /// <summary>
    /// The entity ID serializer allows to parse entities from JSON, or
    /// to format the entity id as JSON string.
    /// </summary>
    public interface IEntityIdSerializer
    {
        /// <summary>
        /// Parse an entity ID from a JSON object.
        /// </summary>
        /// <param name="obj">
        /// The JSON object.
        /// </param>
        /// <returns>
        /// Returns an entity id that can identify the object.
        /// </returns>
        EntityId Parse(JsonElement obj);

        /// <summary>
        /// Formats the <paramref name="entityId"/> to a JSON object.
        /// </summary>
        /// <param name="entityId">
        /// The entity id that shall be serialized.
        /// </param>
        /// <returns>
        /// Returns a string representing the <paramref name="entityId"/>.
        /// </returns>
        string Format(EntityId entityId);
    }
}
