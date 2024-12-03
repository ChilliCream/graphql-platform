#nullable enable
using HotChocolate.Language;

namespace HotChocolate;

/// <summary>
/// A schema document formatter can be used to format/rewrite the
/// schema document that is produced by a <see cref="Schema"/>
/// instance.
/// </summary>
public interface ISchemaDocumentFormatter
{
    /// <summary>
    /// Formats the schema document.
    /// </summary>
    /// <param name="schemaDocument">
    /// The schema document that shall be formatted.
    /// </param>
    /// <returns>
    /// Returns the formatted schema document.
    /// </returns>
    DocumentNode Format(DocumentNode schemaDocument);
}
