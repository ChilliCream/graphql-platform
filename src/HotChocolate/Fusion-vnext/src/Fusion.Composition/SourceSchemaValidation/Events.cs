using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.SourceSchemaValidation;

internal record DirectiveArgumentEvent(
    InputFieldDefinition Argument,
    DirectiveDefinition Directive,
    SchemaDefinition Schema) : IEvent;

internal record FieldArgumentEvent(
    InputFieldDefinition Argument,
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record KeyFieldEvent(
    Directive KeyDirective,
    ComplexTypeDefinition EntityType,
    OutputFieldDefinition Field,
    ComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record KeyFieldNodeEvent(
    FieldNode FieldNode,
    ImmutableArray<string> FieldNamePath,
    Directive KeyDirective,
    ComplexTypeDefinition EntityType,
    SchemaDefinition Schema) : IEvent;

internal record KeyFieldsInvalidReferenceEvent(
    FieldNode FieldNode,
    ComplexTypeDefinition Type,
    Directive KeyDirective,
    ComplexTypeDefinition EntityType,
    SchemaDefinition Schema) : IEvent;

internal record KeyFieldsInvalidSyntaxEvent(
    Directive KeyDirective,
    ComplexTypeDefinition EntityType,
    SchemaDefinition Schema) : IEvent;

internal record KeyFieldsInvalidTypeEvent(
    Directive KeyDirective,
    ComplexTypeDefinition EntityType,
    SchemaDefinition Schema) : IEvent;

internal record OutputFieldEvent(
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record ProvidesFieldEvent(
    OutputFieldDefinition ProvidedField,
    ComplexTypeDefinition ProvidedType,
    Directive ProvidesDirective,
    OutputFieldDefinition Field,
    ComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record ProvidesFieldNodeEvent(
    FieldNode FieldNode,
    ImmutableArray<string> FieldNamePath,
    Directive ProvidesDirective,
    OutputFieldDefinition Field,
    ComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record ProvidesFieldsInvalidSyntaxEvent(
    Directive ProvidesDirective,
    OutputFieldDefinition Field,
    ComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record ProvidesFieldsInvalidTypeEvent(
    Directive ProvidesDirective,
    OutputFieldDefinition Field,
    ComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record RequireFieldNodeEvent(
    FieldNode FieldNode,
    ImmutableArray<string> FieldNamePath,
    Directive RequireDirective,
    InputFieldDefinition Argument,
    OutputFieldDefinition Field,
    ComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record RequireFieldsInvalidSyntaxEvent(
    Directive RequireDirective,
    InputFieldDefinition Argument,
    OutputFieldDefinition Field,
    ComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record RequireFieldsInvalidTypeEvent(
    Directive RequireDirective,
    InputFieldDefinition Argument,
    OutputFieldDefinition Field,
    ComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record SchemaEvent(SchemaDefinition Schema) : IEvent;

internal record TypeEvent(
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;
