#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types.Introspection
{
    /// <summary>
    /// Directive arguments can have names and values.
    /// The values are in graphql SDL syntax printed as a string.
    /// This type is NOT specified by the graphql specification presently.
    /// </summary>
    [Introspection]
    public class __DirectiveArgument : ObjectType<ArgumentNode>
    {
        protected override void Configure(
            IObjectTypeDescriptor<ArgumentNode> descriptor)
        {
            descriptor
                .Name(Names.__DirectiveArgument)
                .Description(TypeResources.___DirectiveArgument_Description)
                // Introspection types must always be bound explicitly so that we
                // do not get any interference with conventions.
                .BindFieldsExplicitly();

            descriptor
                .Field(Names.Name)
                .Type<NonNullType<StringType>>()
                .Resolve(c => c.Parent<ArgumentNode>().Name.Value);

            descriptor
                .Field(Names.Value)
                .Type<NonNullType<StringType>>()
                .Resolve(c => c.Parent<ArgumentNode>().Value.Print(indented: false));
        }

        public static class Names
        {
            public const string __DirectiveArgument = "__DirectiveArgument";
            public const string Name = "name";
            public const string Value = "value";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
