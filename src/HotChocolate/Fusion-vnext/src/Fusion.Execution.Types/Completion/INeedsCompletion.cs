namespace HotChocolate.Fusion.Types.Completion;

internal interface INeedsCompletion
{
    void Complete(FusionSchemaDefinition schema, CompositeSchemaBuilderContext context);
}
