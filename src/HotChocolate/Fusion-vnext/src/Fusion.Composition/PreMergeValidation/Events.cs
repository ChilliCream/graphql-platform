using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation;

internal record EachDirectiveArgumentEvent(
    CompositionContext Context,
    InputFieldDefinition Argument,
    DirectiveDefinition Directive,
    SchemaDefinition Schema) : IEvent;

internal record EachDirectiveEvent(
    CompositionContext Context,
    DirectiveDefinition Directive,
    SchemaDefinition Schema) : IEvent;

internal record EachFieldArgumentEvent(
    CompositionContext Context,
    InputFieldDefinition Argument,
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record EachFieldArgumentGroupEvent(
    CompositionContext Context,
    string ArgumentName,
    ImmutableArray<FieldArgumentInfo> ArgumentGroup,
    string FieldName,
    string TypeName) : IEvent;

internal record EachOutputFieldEvent(
    CompositionContext Context,
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record EachOutputFieldGroupEvent(
    CompositionContext Context,
    string FieldName,
    ImmutableArray<OutputFieldInfo> FieldGroup,
    string TypeName) : IEvent;

internal record EachTypeEvent(
    CompositionContext Context,
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record EachTypeGroupEvent(
    CompositionContext Context,
    string TypeName,
    ImmutableArray<TypeInfo> TypeGroup) : IEvent;
