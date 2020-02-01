namespace HotChocolate.Types
{
    /// <summary>
    /// The @deprecated directive is used within the type system definition
    /// language to indicate deprecated portions of a GraphQL service’s schema,
    /// such as deprecated fields on a type or deprecated enum values.
    ///
    /// Deprecations include a reason for why it is deprecated,
    /// which is formatted using Markdown syntax (as specified by CommonMark).
    /// </summary>
    public sealed class DeprecatedDirectiveType
        : DirectiveType<DeprecatedDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<DeprecatedDirective> descriptor)
        {
            // TODO : resources
            descriptor
                .Name("deprecated")
                .Description(
                    "The @deprecated directive is used within the " +
                    "type system definition language to indicate " +
                    "deprecated portions of a GraphQL service’s schema," +
                    "such as deprecated fields on a type or deprecated " +
                    "enum values.")
                .Location(DirectiveLocation.FieldDefinition)
                .Location(DirectiveLocation.EnumValue)
                .Argument(t => t.Reason)
                .Name("reason")
                .Description(
                    "Deprecations include a reason for why it is deprecated, " +
                    "which is formatted using Markdown syntax " +
                    "(as specified by CommonMark).")
                .Type<StringType>()
                .DefaultValue(WellKnownDirectives.DeprecationDefaultReason);
        }
    }
}
