using System.Collections.Immutable;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Info;

namespace HotChocolate.Fusion.Events;

internal record EnumTypeGroupEvent(
    string TypeName,
    ImmutableArray<EnumTypeInfo> TypeGroup) : IEvent;

internal record FieldArgumentGroupEvent(
    string ArgumentName,
    ImmutableArray<FieldArgumentInfo> ArgumentGroup,
    string FieldName,
    string TypeName) : IEvent;

internal record InputFieldGroupEvent(
    string FieldName,
    ImmutableArray<InputFieldInfo> FieldGroup,
    string TypeName) : IEvent;

internal record InputTypeGroupEvent(
    string InputTypeName,
    ImmutableArray<InputTypeInfo> InputTypeGroup) : IEvent;

internal record OutputFieldGroupEvent(
    string FieldName,
    ImmutableArray<OutputFieldInfo> FieldGroup,
    string TypeName) : IEvent;

internal record TypeGroupEvent(
    string TypeName,
    ImmutableArray<TypeInfo> TypeGroup) : IEvent;
