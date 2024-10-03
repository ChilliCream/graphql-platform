using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Execution.Projections;

internal sealed class RequirementsTypeInterceptor : TypeInterceptor
{
    private readonly FieldRequirementsMetadata _metadata = new();

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is SchemaTypeDefinition schema)
        {
            schema.Features.Set(_metadata);
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is not ObjectTypeDefinition typeDef)
        {
            return;
        }

        var runtimeType = typeDef.RuntimeType != typeof(object) ? typeDef.RuntimeType : null;

        foreach (var fieldDef in typeDef.Fields)
        {
            if((fieldDef.Flags & FieldFlags.WithRequirements) == FieldFlags.WithRequirements)
            {
                var fieldCoordinate = new SchemaCoordinate(
                    typeDef.Name,
                    fieldDef.Name);

                // if the source generator already compiled the
                // requirements we will take it and skip compilation.
                if (fieldDef.ContextData.TryGetValue(FieldRequirements, out var value))
                {
                    _metadata.TryAddRequirements(fieldCoordinate, (TypeNode)value!);
                    continue;
                }

                var requirements = (string)fieldDef.ContextData[FieldRequirementsSyntax]!;
                var entityType = runtimeType ?? (Type)fieldDef.ContextData[FieldRequirementsEntity]!;

                var propertyNodes = PropertyTreeBuilder.Build(
                    fieldCoordinate,
                    entityType,
                    requirements);

                _metadata.TryAddRequirements(fieldCoordinate, propertyNodes);
            }
        }
    }

    internal override void OnAfterCreateSchemaInternal(
        IDescriptorContext context,
        ISchema schema)
        => _metadata.Seal();
}
