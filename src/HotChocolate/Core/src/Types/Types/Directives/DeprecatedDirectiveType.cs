using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// The @deprecated directive is used within the type system definition
/// language to indicate deprecated portions of a GraphQL service’s schema,
/// such as deprecated fields on a type or deprecated enum values.
///
/// Deprecations include a reason for why it is deprecated,
/// which is formatted using Markdown syntax (as specified by CommonMark).
/// </summary>
public sealed class DeprecatedDirectiveType : DirectiveType<DeprecatedDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<DeprecatedDirective> descriptor)
    {
        descriptor
            .Name(WellKnownDirectives.Deprecated)
            .Description(TypeResources.DeprecatedDirectiveType_TypeDescription)
            .Location(DirectiveLocation.FieldDefinition)
            .Location(DirectiveLocation.ArgumentDefinition)
            .Location(DirectiveLocation.InputFieldDefinition)
            .Location(DirectiveLocation.EnumValue);

        descriptor
            .Argument(t => t.Reason)
            .Name(WellKnownDirectives.DeprecationReasonArgument)
            .Description(TypeResources.DeprecatedDirectiveType_ReasonDescription)
            .Type<StringType>()
            .DefaultValue(WellKnownDirectives.DeprecationDefaultReason);
    }
}
