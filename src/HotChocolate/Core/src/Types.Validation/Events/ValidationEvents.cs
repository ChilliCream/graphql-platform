using HotChocolate.Events.Contracts;
using HotChocolate.Types;

namespace HotChocolate.Events;

/// <summary>
/// Represents an event that is triggered when an argument is encountered during schema validation.
/// </summary>
public sealed record ArgumentEvent(IInputValueDefinition Argument) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a complex type is encountered during schema validation.
/// </summary>
public sealed record ComplexTypeEvent(IComplexTypeDefinition ComplexType) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a directive is encountered during schema validation.
/// </summary>
public sealed record DirectiveEvent(IDirectiveDefinition Directive) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an Enum type is encountered during schema validation.
/// </summary>
public sealed record EnumTypeEvent(IEnumTypeDefinition EnumType) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an enum value is encountered during schema validation.
/// </summary>
public sealed record EnumValueEvent(IEnumValue EnumValue) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a field is encountered during schema validation.
/// </summary>
public sealed record FieldEvent(IFieldDefinition Field) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an Input Object field is encountered during schema validation.
/// </summary>
public sealed record InputFieldEvent(IInputValueDefinition InputField) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an Input Object type is encountered during schema validation.
/// </summary>
public sealed record InputObjectTypeEvent(IInputObjectTypeDefinition InputObjectType) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when multiple Input Object types are encountered during schema validation.
/// </summary>
public sealed record InputObjectTypesEvent(IEnumerable<IInputObjectTypeDefinition> InputObjectTypes) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an input value is encountered during schema validation.
/// </summary>
public sealed record InputValueEvent(IInputValueDefinition InputValue) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an Interface type is encountered during schema validation.
/// </summary>
public sealed record InterfaceTypeEvent(IInterfaceTypeDefinition InterfaceType) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a named member is encountered during schema validation.
/// </summary>
public sealed record NamedMemberEvent(INameProvider NamedMember) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an Object type is encountered during schema validation.
/// </summary>
public sealed record ObjectTypeEvent(IObjectTypeDefinition ObjectType) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an output field is encountered during schema validation.
/// </summary>
public sealed record OutputFieldEvent(IOutputFieldDefinition OutputField) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a type is encountered during schema validation.
/// </summary>
public sealed record TypeEvent(ITypeDefinition Type) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a Union type is encountered during schema validation.
/// </summary>
public sealed record UnionTypeEvent(IUnionTypeDefinition UnionType) : IValidationEvent;
