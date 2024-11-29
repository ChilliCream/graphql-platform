using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate;

internal sealed class MapMiddleware
{
    private readonly FieldDelegate _next;
    private readonly FieldReference _fieldReference;
    private readonly FieldDelegate _fieldDelegate;

    public MapMiddleware(
        FieldDelegate next,
        FieldReference fieldReference,
        FieldDelegate fieldDelegate)
    {
        _next = next
            ?? throw new ArgumentNullException(nameof(next));
        _fieldReference = fieldReference
            ?? throw new ArgumentNullException(nameof(fieldReference));
        _fieldDelegate = fieldDelegate
            ?? throw new ArgumentNullException(nameof(fieldDelegate));
    }

    public ValueTask InvokeAsync(IMiddlewareContext context)
    {
        return IsField(context.ObjectType.Name, context.Selection.Field.Name)
            ? _fieldDelegate(context)
            : _next(context);
    }

    private bool IsField(string typeName, string fieldName)
    {
        return _fieldReference.TypeName.EqualsOrdinal(typeName)
            && _fieldReference.FieldName.EqualsOrdinal(fieldName);
    }
}
