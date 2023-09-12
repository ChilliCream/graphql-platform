using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class AlignTypesVisitor(Schema schema) : SchemaVisitor<object>
{
    public override void VisitOutputField(OutputField field, object context)
    {
        var type = field.Type.NamedType();
        
        if (schema.Types.TryGetType(type.Name, out var schemaType) && 
            !schemaType.Equals(type, TypeComparison.Reference))
        {
            field.Type = field.Type.ReplaceNameType(_ => schemaType);
        }
        
        base.VisitOutputField(field, context);
    }

    public override void VisitInputField(InputField field, object context)
    {
        var type = field.Type.NamedType();
        
        if (schema.Types.TryGetType(type.Name, out var schemaType) && 
            !schemaType.Equals(type, TypeComparison.Reference))
        {
            field.Type = field.Type.ReplaceNameType(_ => schemaType);
        }
        
        base.VisitInputField(field, context);
    }
}