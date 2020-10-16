using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// The `@stream` directive may be provided for a field of `List` type so that the
    /// backend can leverage technology such as asynchronous iterators to provide a partial
    /// list in the initial response, and additional list items in subsequent responses.
    /// `@include` and `@skip` take precedence over `@stream`.
    ///
    /// directive @stream(label: String, initialCount: Int!, if: Boolean) on FIELD
    /// </summary>
    public class StreamDirectiveType
        : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name("stream")
                .Description(TypeResources.StreamDirectiveType_Description)
                .Location(DirectiveLocation.Field);

            descriptor
                .Argument("label")
                .Description(TypeResources.StreamDirectiveType_Label_Description)
                .Type<StringType>();

            descriptor
                .Argument("initialCount")
                .Description(TypeResources.StreamDirectiveType_InitialCount_Description)
                .Type<NonNullType<IntType>>();

            descriptor
                .Argument(WellKnownDirectives.IfArgument)
                .Description(TypeResources.StreamDirectiveType_If_Description)
                .Type<NonNullType<BooleanType>>();
        }
    }
}
