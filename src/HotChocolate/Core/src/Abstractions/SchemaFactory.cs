using HotChocolate.Language;

namespace HotChocolate;

/// <summary>
/// Implement this interface to enable design-time services to create the GraphQL type system.
/// </summary>
public interface IDesignTimeSchemaDocumentFactory
{
    DocumentNode CreateSchemaDocument(string[] args);
}
