#nullable enable
using HotChocolate.Language;

namespace HotChocolate;

internal sealed class AggregateSchemaDocumentFormatter(
    IEnumerable<ISchemaDocumentFormatter>? formatters)
    : ISchemaDocumentFormatter
{
    private readonly ISchemaDocumentFormatter[] _formatters = formatters?.ToArray() ?? [];

    public DocumentNode Format(DocumentNode schemaDocument)
    {
        if(_formatters.Length == 0)
        {
            return schemaDocument;
        }

        if(_formatters.Length == 1)
        {
            return _formatters[0].Format(schemaDocument);
        }

        var current = schemaDocument;

        for (var i = 0; i < _formatters.Length; i++)
        {
            current = _formatters[i].Format(current);
        }

        return current;
    }
}
