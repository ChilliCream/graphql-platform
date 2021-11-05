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
    public class StreamDirectiveType : DirectiveType<StreamDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<StreamDirective> descriptor)
        {
            descriptor
                .Name(Names.Stream)
                .Description(TypeResources.StreamDirectiveType_Description)
                .Location(DirectiveLocation.Field);

            descriptor
                .Argument(t => t.Label)
                .Name(Names.Label)
                .Description(TypeResources.StreamDirectiveType_Label_Description)
                .Type<StringType>();

            descriptor
                .Argument(t => t.InitialCount)
                .Name(Names.InitialCount)
                .Description(TypeResources.StreamDirectiveType_InitialCount_Description)
                .Type<NonNullType<IntType>>()
                .DefaultValue(0);

            descriptor
                .Argument(t => t.If)
                .Name(Names.If)
                .Description(TypeResources.StreamDirectiveType_If_Description)
                .Type<BooleanType>();
        }

        public static class Names
        {
            public const string Stream = "stream";
            public const string Label = "label";
            public const string InitialCount = "initialCount";
            public const string If = "if";
        }
    }
}
