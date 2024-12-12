using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation;

internal record EachDirectiveArgumentEvent(
    InputFieldDefinition Argument,
    DirectiveDefinition Directive,
    SchemaDefinition Schema) : IEvent;

internal record EachDirectiveEvent(
    DirectiveDefinition Directive,
    SchemaDefinition Schema) : IEvent;

internal record EachFieldArgumentEvent(
    InputFieldDefinition Argument,
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record EachFieldArgumentGroupEvent(
    string ArgumentName,
    ImmutableArray<FieldArgumentInfo> ArgumentGroup,
    string FieldName,
    string TypeName) : IEvent;

internal record EachOutputFieldEvent(
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record EachOutputFieldGroupEvent(
    string FieldName,
    ImmutableArray<OutputFieldInfo> FieldGroup,
    string TypeName) : IEvent;

internal record EachTypeEvent(
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record EachTypeGroupEvent(
    string TypeName,
    ImmutableArray<TypeInfo> TypeGroup) : IEvent;
