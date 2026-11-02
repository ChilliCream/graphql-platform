using HotChocolate.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Types;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Events;

/// <summary>
/// Represents an event that is triggered when an argument is encountered during schema validation.
/// </summary>
public readonly record struct ArgumentEvent(IInputValueDefinition Argument) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a complex type is encountered during schema validation.
/// </summary>
public readonly record struct ComplexTypeEvent(IComplexTypeDefinition ComplexType) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered for each node of a default value literal
/// encountered during schema validation.
/// </summary>
/// <param name="Value">The default value literal node at the current position.</param>
/// <param name="Type">The type expected at the current position.</param>
/// <param name="Path">The path from the default value root to the current node (input field names and list indices).</param>
/// <param name="Root">The argument or input field that declares the default value.</param>
public readonly record struct DefaultValueNodeEvent(
    IValueNode Value,
    IType Type,
    IReadOnlyList<object> Path,
    IInputValueDefinition Root) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a directive argument assignment is encountered during schema validation.
/// </summary>
public readonly record struct DirectiveArgumentAssignmentEvent(
    ArgumentAssignment Assignment,
    IInputValueDefinition Argument,
    IDirective Directive,
    ITypeSystemMember Member) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a directive definition is encountered during schema validation.
/// </summary>
public readonly record struct DirectiveDefinitionEvent(IDirectiveDefinition DirectiveDefinition) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a directive is encountered during schema validation.
/// </summary>
/// <param name="Directive">The applied directive.</param>
/// <param name="Member">The type system member the directive is applied to.</param>
/// <param name="Location">The location at which the directive is applied.</param>
public readonly record struct DirectiveEvent(
    IDirective Directive,
    ITypeSystemMember Member,
    DirectiveLocation Location) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an Enum type is encountered during schema validation.
/// </summary>
public readonly record struct EnumTypeEvent(IEnumTypeDefinition EnumType) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an enum value is encountered during schema validation.
/// </summary>
public readonly record struct EnumValueEvent(IEnumValue EnumValue) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a field is encountered during schema validation.
/// </summary>
public readonly record struct FieldEvent(IFieldDefinition Field) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an Input Object field is encountered during schema validation.
/// </summary>
public readonly record struct InputFieldEvent(IInputValueDefinition InputField) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an Input Object type is encountered during schema validation.
/// </summary>
public readonly record struct InputObjectTypeEvent(IInputObjectTypeDefinition InputObjectType) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when multiple Input Object types are encountered during schema validation.
/// </summary>
public readonly record struct InputObjectTypesEvent(IEnumerable<IInputObjectTypeDefinition> InputObjectTypes) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an input value is encountered during schema validation.
/// </summary>
public readonly record struct InputValueEvent(IInputValueDefinition InputValue) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an Interface type is encountered during schema validation.
/// </summary>
public readonly record struct InterfaceTypeEvent(IInterfaceTypeDefinition InterfaceType) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a named member is encountered during schema validation.
/// </summary>
public readonly record struct NamedMemberEvent(INameProvider NamedMember) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an Object type is encountered during schema validation.
/// </summary>
public readonly record struct ObjectTypeEvent(IObjectTypeDefinition ObjectType) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when an output field is encountered during schema validation.
/// </summary>
public readonly record struct OutputFieldEvent(IOutputFieldDefinition OutputField) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a type is encountered during schema validation.
/// </summary>
public readonly record struct TypeEvent(ITypeDefinition Type) : IValidationEvent;

/// <summary>
/// Represents an event that is triggered when a Union type is encountered during schema validation.
/// </summary>
public readonly record struct UnionTypeEvent(IUnionTypeDefinition UnionType) : IValidationEvent;
