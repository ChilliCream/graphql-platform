using System.Collections.Immutable;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Events;

internal record DirectiveArgumentEvent(
    MutableInputFieldDefinition Argument,
    MutableDirectiveDefinition MutableDirective,
    SchemaDefinition Schema) : IEvent;

internal record EnumTypeEvent(
    MutableEnumTypeDefinition MutableEnumType,
    SchemaDefinition Schema) : IEvent;

internal record FieldArgumentEvent(
    MutableInputFieldDefinition Argument,
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record InputFieldEvent(
    MutableInputFieldDefinition MutableInputField,
    InputObjectTypeDefinition InputType,
    SchemaDefinition Schema) : IEvent;

internal record InputTypeEvent(
    InputObjectTypeDefinition InputType,
    SchemaDefinition Schema) : IEvent;

internal record InterfaceTypeEvent(
    InterfaceTypeDefinition InterfaceType,
    SchemaDefinition Schema) : IEvent;

internal record KeyFieldEvent(
    Directive KeyDirective,
    MutableComplexTypeDefinition EntityType,
    OutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record KeyFieldNodeEvent(
    FieldNode FieldNode,
    ImmutableArray<string> FieldNamePath,
    Directive KeyDirective,
    MutableComplexTypeDefinition EntityType,
    SchemaDefinition Schema) : IEvent;

internal record KeyFieldsInvalidReferenceEvent(
    FieldNode FieldNode,
    MutableComplexTypeDefinition Type,
    Directive KeyDirective,
    MutableComplexTypeDefinition EntityType,
    SchemaDefinition Schema) : IEvent;

internal record KeyFieldsInvalidSyntaxEvent(
    Directive KeyDirective,
    MutableComplexTypeDefinition EntityType,
    SchemaDefinition Schema) : IEvent;

internal record KeyFieldsInvalidTypeEvent(
    Directive KeyDirective,
    MutableComplexTypeDefinition EntityType,
    SchemaDefinition Schema) : IEvent;

internal record ObjectTypeEvent(
    ObjectTypeDefinition ObjectType,
    SchemaDefinition Schema) : IEvent;

internal record OutputFieldEvent(
    OutputFieldDefinition Field,
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record ProvidesFieldEvent(
    OutputFieldDefinition ProvidedField,
    MutableComplexTypeDefinition ProvidedType,
    Directive ProvidesDirective,
    OutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record ProvidesFieldNodeEvent(
    FieldNode FieldNode,
    ImmutableArray<string> FieldNamePath,
    Directive ProvidesDirective,
    OutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record ProvidesFieldsInvalidSyntaxEvent(
    Directive ProvidesDirective,
    OutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record ProvidesFieldsInvalidTypeEvent(
    Directive ProvidesDirective,
    OutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record RequireFieldNodeEvent(
    FieldNode FieldNode,
    ImmutableArray<string> FieldNamePath,
    Directive RequireDirective,
    MutableInputFieldDefinition Argument,
    OutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record RequireFieldsInvalidSyntaxEvent(
    Directive RequireDirective,
    MutableInputFieldDefinition Argument,
    OutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record RequireFieldsInvalidTypeEvent(
    Directive RequireDirective,
    MutableInputFieldDefinition Argument,
    OutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record SchemaEvent(SchemaDefinition Schema) : IEvent;

internal record TypeEvent(
    INamedTypeDefinition Type,
    SchemaDefinition Schema) : IEvent;

internal record UnionTypeEvent(
    UnionTypeDefinition UnionType,
    SchemaDefinition Schema) : IEvent;
