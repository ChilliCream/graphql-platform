using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Execution.Requirements;

internal sealed class RequirementsTypeInterceptor : TypeInterceptor
{
    private readonly FieldRequirementsMetadata _metadata = new();

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (configuration is SchemaTypeConfiguration schema)
        {
            schema.Features.Set(_metadata);
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (configuration is not ObjectTypeConfiguration typeDef)
        {
            return;
        }

        var runtimeType = typeDef.RuntimeType != typeof(object) ? typeDef.RuntimeType : null;

        foreach (var fieldDef in typeDef.Fields)
        {
            if ((fieldDef.Flags & CoreFieldFlags.WithRequirements) == CoreFieldFlags.WithRequirements)
            {
                var fieldCoordinate = new SchemaCoordinate(typeDef.Name, fieldDef.Name);
                var feature = fieldDef.Features.GetRequired<FieldRequirementFeature>();
                var requirements = feature.Requirements;
                var entityType = runtimeType ?? feature.EntityType;

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
        Schema schema)
        => _metadata.Seal();
}
