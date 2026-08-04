using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Events;

internal record ComplexTypeEvent(
    MutableComplexTypeDefinition ComplexType,
    MutableSchemaDefinition Schema) : IEvent;

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

internal record ObjectTypeEvent(
    MutableObjectTypeDefinition ObjectType,
    MutableSchemaDefinition Schema) : IEvent;

internal record OutputFieldEvent(
    MutableOutputFieldDefinition Field,
    ITypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record SchemaEvent(MutableSchemaDefinition Schema) : IEvent;

internal record TypeEvent(
    ITypeDefinition Type,
    MutableSchemaDefinition Schema) : IEvent;

internal record UnionTypeEvent(
    MutableUnionTypeDefinition UnionType,
    MutableSchemaDefinition Schema) : IEvent;
