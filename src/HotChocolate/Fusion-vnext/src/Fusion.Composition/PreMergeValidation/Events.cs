using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.PreMergeValidation;

internal record DirectiveArgumentEvent(
    InputFieldDefinition Argument,
    DirectiveDefinition Directive,
    SchemaDefinition Schema) : IEvent;

internal record DirectiveEvent(
    DirectiveDefinition Directive,
    SchemaDefinition Schema) : IEvent;

internal record FieldArgumentEvent(
    InputFieldDefinition Argument,
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record FieldArgumentGroupEvent(
    string ArgumentName,
    ImmutableArray<FieldArgumentInfo> ArgumentGroup,
    string FieldName,
    string TypeName) : IEvent;

internal record KeyFieldEvent(
    ComplexTypeDefinition EntityType,
    Directive KeyDirective,
    OutputFieldDefinition Field,
    ComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record KeyFieldNodeEvent(
    ComplexTypeDefinition EntityType,
    Directive KeyDirective,
    FieldNode FieldNode,
    ImmutableArray<string> FieldNamePath,
    SchemaDefinition Schema) : IEvent;

internal record OutputFieldEvent(
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record OutputFieldGroupEvent(
    string FieldName,
    ImmutableArray<OutputFieldInfo> FieldGroup,
    string TypeName) : IEvent;

internal record SchemaEvent(SchemaDefinition Schema) : IEvent;

internal record TypeEvent(
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record TypeGroupEvent(
    string TypeName,
    ImmutableArray<TypeInfo> TypeGroup) : IEvent;
