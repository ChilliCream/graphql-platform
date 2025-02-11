using System.Collections.Immutable;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Events;

internal record DirectiveArgumentEvent(
    MutableInputFieldDefinition Argument,
    MutableDirectiveDefinition MutableDirective,
    MutableSchemaDefinition Schema) : IEvent;

internal record EnumTypeEvent(
    MutableEnumTypeDefinition MutableEnumType,
    MutableSchemaDefinition Schema) : IEvent;

internal record FieldArgumentEvent(
    MutableInputFieldDefinition Argument,
    MutableOutputFieldDefinition Field,
    INamedTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record InputFieldEvent(
    MutableInputFieldDefinition MutableInputField,
    InputObjectTypeDefinition InputType,
    MutableSchemaDefinition Schema) : IEvent;

internal record InputTypeEvent(
    InputObjectTypeDefinition InputType,
    MutableSchemaDefinition Schema) : IEvent;

internal record InterfaceTypeEvent(
    MutableInterfaceTypeDefinition InterfaceType,
    MutableSchemaDefinition Schema) : IEvent;

internal record KeyFieldEvent(
    Directive KeyDirective,
    MutableComplexTypeDefinition EntityType,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record KeyFieldNodeEvent(
    FieldNode FieldNode,
    ImmutableArray<string> FieldNamePath,
    Directive KeyDirective,
    MutableComplexTypeDefinition EntityType,
    MutableSchemaDefinition Schema) : IEvent;

internal record KeyFieldsInvalidReferenceEvent(
    FieldNode FieldNode,
    MutableComplexTypeDefinition Type,
    Directive KeyDirective,
    MutableComplexTypeDefinition EntityType,
    MutableSchemaDefinition Schema) : IEvent;

internal record KeyFieldsInvalidSyntaxEvent(
    Directive KeyDirective,
    MutableComplexTypeDefinition EntityType,
    MutableSchemaDefinition Schema) : IEvent;

internal record KeyFieldsInvalidTypeEvent(
    Directive KeyDirective,
    MutableComplexTypeDefinition EntityType,
    MutableSchemaDefinition Schema) : IEvent;

internal record ObjectTypeEvent(
    MutableObjectTypeDefinition ObjectType,
    MutableSchemaDefinition Schema) : IEvent;

internal record OutputFieldEvent(
    MutableOutputFieldDefinition Field,
    INamedTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record ProvidesFieldEvent(
    MutableOutputFieldDefinition ProvidedField,
    MutableComplexTypeDefinition ProvidedType,
    Directive ProvidesDirective,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record ProvidesFieldNodeEvent(
    FieldNode FieldNode,
    ImmutableArray<string> FieldNamePath,
    Directive ProvidesDirective,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record ProvidesFieldsInvalidSyntaxEvent(
    Directive ProvidesDirective,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record ProvidesFieldsInvalidTypeEvent(
    Directive ProvidesDirective,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record RequireFieldNodeEvent(
    FieldNode FieldNode,
    ImmutableArray<string> FieldNamePath,
    Directive RequireDirective,
    MutableInputFieldDefinition Argument,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record RequireFieldsInvalidSyntaxEvent(
    Directive RequireDirective,
    MutableInputFieldDefinition Argument,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record RequireFieldsInvalidTypeEvent(
    Directive RequireDirective,
    MutableInputFieldDefinition Argument,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record SchemaEvent(MutableSchemaDefinition Schema) : IEvent;

internal record TypeEvent(
    INamedTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record UnionTypeEvent(
    MutableUnionTypeDefinition UnionType,
    MutableSchemaDefinition Schema) : IEvent;
