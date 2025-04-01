using System.Collections.Immutable;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Events;

internal record DirectiveArgumentEvent(
    MutableInputFieldDefinition Argument,
    MutableDirectiveDefinition Directive,
    MutableSchemaDefinition Schema) : IEvent;

internal record EnumTypeEvent(
    MutableEnumTypeDefinition EnumType,
    MutableSchemaDefinition Schema) : IEvent;

internal record FieldArgumentEvent(
    MutableInputFieldDefinition Argument,
    MutableOutputFieldDefinition Field,
    ITypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record InputFieldEvent(
    MutableInputFieldDefinition InputField,
    MutableInputObjectTypeDefinition InputType,
    MutableSchemaDefinition Schema) : IEvent;

internal record InputTypeEvent(
    MutableInputObjectTypeDefinition InputType,
    MutableSchemaDefinition Schema) : IEvent;

internal record InterfaceTypeEvent(
    MutableInterfaceTypeDefinition InterfaceType,
    MutableSchemaDefinition Schema) : IEvent;

internal record IsFieldInvalidSyntaxEvent(
    Directive IsDirective,
    MutableInputFieldDefinition Argument,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record IsFieldInvalidTypeEvent(
    Directive IsDirective,
    MutableInputFieldDefinition Argument,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record KeyFieldEvent(
    MutableOutputFieldDefinition KeyField,
    MutableComplexTypeDefinition KeyFieldDeclaringType,
    Directive KeyDirective,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record KeyFieldNodeEvent(
    FieldNode FieldNode,
    ImmutableArray<string> FieldNamePath,
    Directive KeyDirective,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record KeyFieldsEvent(
    SelectionSetNode SelectionSet,
    Directive KeyDirective,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record KeyFieldsInvalidSyntaxEvent(
    Directive KeyDirective,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record KeyFieldsInvalidTypeEvent(
    Directive KeyDirective,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record ObjectTypeEvent(
    MutableObjectTypeDefinition ObjectType,
    MutableSchemaDefinition Schema) : IEvent;

internal record OutputFieldEvent(
    MutableOutputFieldDefinition Field,
    ITypeDefinition Type,
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

internal record ProvidesFieldsEvent(
    SelectionSetNode SelectionSet,
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

internal record RequireFieldInvalidSyntaxEvent(
    Directive RequireDirective,
    MutableInputFieldDefinition Argument,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record RequireFieldInvalidTypeEvent(
    Directive RequireDirective,
    MutableInputFieldDefinition Argument,
    MutableOutputFieldDefinition Field,
    MutableComplexTypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record SchemaEvent(MutableSchemaDefinition Schema) : IEvent;

internal record TypeEvent(
    ITypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record UnionTypeEvent(
    MutableUnionTypeDefinition UnionType,
    MutableSchemaDefinition Schema) : IEvent;
