using System.Collections.Immutable;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation;

internal record EachDirectiveArgumentEvent(
    CompositionContext Context,
    InputFieldDefinition Argument,
    DirectiveDefinition Directive,
    SchemaDefinition Schema);

internal record EachDirectiveEvent(
    CompositionContext Context,
    DirectiveDefinition Directive,
    SchemaDefinition Schema);

internal record EachFieldArgumentEvent(
    CompositionContext Context,
    InputFieldDefinition Argument,
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema);

internal record EachFieldArgumentNameEvent(
    CompositionContext Context,
    string ArgumentName,
    ImmutableArray<FieldArgumentInfo> ArgumentInfo,
    string FieldName,
    string TypeName);

internal record EachOutputFieldEvent(
    CompositionContext Context,
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema);

internal record EachOutputFieldNameEvent(
    CompositionContext Context,
    string FieldName,
    ImmutableArray<OutputFieldInfo> FieldInfo,
    string TypeName);

internal record EachTypeEvent(
    CompositionContext Context,
    INamedTypeDefinition Type,
    SchemaDefinition Schema);

internal record EachTypeNameEvent(
    CompositionContext Context,
    string TypeName,
    ImmutableArray<TypeInfo> TypeInfo);
