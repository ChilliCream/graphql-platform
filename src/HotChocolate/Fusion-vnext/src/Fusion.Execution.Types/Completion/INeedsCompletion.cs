namespace HotChocolate.Fusion.Types.Completion;

/// <summary>
/// Can be implemented by type system members or type system features,
/// to be notified when the schema is object is completed.
/// </summary>
internal interface INeedsCompletion
{
    void Complete(
        FusionSchemaDefinition schema,
        CompositeSchemaBuilderContext context);
}
