using System;
using HotChocolate.ApolloFederation.Extensions;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// This attribute is used to mark types as an extended type
/// of a type that is defined by another service when
/// using apollo federation.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Interface)]
public sealed class ExtendServiceTypeAttribute : ObjectTypeDescriptorAttribute
{
    public override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
        => descriptor.ExtendServiceType();
}
