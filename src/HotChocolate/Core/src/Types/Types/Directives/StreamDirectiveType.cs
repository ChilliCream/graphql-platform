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
                .Description(
                    "The `@stream` directive may be provided for a field of `List` " + 
                    "type so that the backend can leverage technology such as " + 
                    "asynchronous iterators to provide a partial list in the initial " + 
                    "response, and additional list items in subsequent responses. "+
                    "`@include` and `@skip` take precedence over `@stream`.")
                .Location(DirectiveLocation.Field);

            descriptor
                .Argument("label")
                .Description(
                    "If this argument label has a value other than null, it will be passed " +
                    "on to the result of this stream directive. This label is intended to " +
                    "give client applications a way to identify to which fragment a streamed " +
                    "result belongs to.")
                .Type<StringType>();

            descriptor
                .Argument("initialCount")
                .Description("The initial elements that shall be send down to the consumer.")
                .Type<NonNullType<IntType>>();

            descriptor
                .Argument(WellKnownDirectives.IfArgument)
                .Description("Streamed when true.")
                .Type<NonNullType<BooleanType>>();
        }
    }
}
